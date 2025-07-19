
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public static class FlowfieldGridStorage
{
    public static NativeArray<FlowfieldCellData> flowfieldGrid;
    public static float2 gridCenter;
    public static int2 gridSize;

    public static FlowfieldCellData GetCellFromPosition(float3 position)
    {
        float2 positionOnGrid = (new float2(position.x, position.y) - gridCenter) + (float2)gridSize * 0.5f;
        int2 cellCoords = new int2(Mathf.Clamp(Mathf.FloorToInt(positionOnGrid.x), 0, gridSize.x-1), Mathf.Clamp(Mathf.FloorToInt(positionOnGrid.y), 0, gridSize.y-1));
        return flowfieldGrid[cellCoords.x+ cellCoords.y*gridSize.x];
    }
}

public partial struct FlowfieldSystem : ISystem
{

    private EntityQuery FlowfieldQuery;
    private EntityQuery TargetQuery;

    /// <summary>
    /// Change this if static bodies are added
    /// </summary>
    private bool RequiresRebuild;

    private NativeArray<FlowfieldCellData> flowfieldGrid;
    private int2 gridSize;
    private ushort cellNum;
    private ushort innerCellNum;

    /// <summary>
    /// To pass to the shader
    /// </summary>
    private NativeArray<FlowfieldCellGPU> flowfieldGridGPU;

    private float2 gridCenter;
    private int2 targetCell;
    private NativeArray<bool> visitedCells;
    private NativeQueue<int2> visibleCellsToVisit;
    private NativeQueue<(int2, float2)> obstructedCellsToVisit;
    ushort currentEvaluatedCost;
    ushort visibleBatchSize;
    ushort obstructedBatchSize;

    public void OnCreate(ref SystemState state)
    {
        RequiresRebuild = true;
        FlowfieldQuery = state.EntityManager.CreateEntityQuery(typeof(FlowfieldTag));
        TargetQuery = state.EntityManager.CreateEntityQuery(typeof(PlayerData));
        state.RequireForUpdate<PlayerData>();
    }

    public void OnDestroy(ref SystemState state)
    {
        if (flowfieldGrid.IsCreated)
        {
            flowfieldGrid.Dispose();
            flowfieldGridGPU.Dispose();
        }
    }

    public void OnUpdate(ref SystemState state)
    {
        float3 targetPosition = state.EntityManager.GetComponentData<LocalToWorld>(TargetQuery.GetSingletonEntity()).Value.Translation();
        float2 targetPositionOnGrid = (new float2(targetPosition.x, targetPosition.y) - gridCenter) + (float2)gridSize * 0.5f;
        int2 updatedTargetCell = new int2(Mathf.FloorToInt(targetPositionOnGrid.x), Mathf.FloorToInt(targetPositionOnGrid.y));
        /// Clamp the target cell to land on the grid
        updatedTargetCell = new int2(Mathf.Clamp(updatedTargetCell.x, 1, gridSize.x-2), Mathf.Clamp(updatedTargetCell.y, 1, gridSize.y-2));

        if (RequiresRebuild)
        {
            RebuildGrid(ref state);
            targetPositionOnGrid = (new float2(targetPosition.x, targetPosition.y) - gridCenter) + (float2)gridSize * 0.5f;
            updatedTargetCell = new int2(Mathf.FloorToInt(targetPositionOnGrid.x), Mathf.FloorToInt(targetPositionOnGrid.y));
            /// Clamp the target cell to land on the grid
            updatedTargetCell = new int2(Mathf.Clamp(updatedTargetCell.x, 1, gridSize.x-2), Mathf.Clamp(updatedTargetCell.y, 1, gridSize.y-2));
            targetCell = updatedTargetCell;

            UpdateGrid(ref state);

            RequiresRebuild = false;
        }
        else
        {
            if(!updatedTargetCell.Equals(targetCell))
            {
                targetCell = updatedTargetCell;
                UpdateGrid(ref state);
            }
        }

        ComputeBuffer buffer = new ComputeBuffer(
            flowfieldGridGPU.Length,
            UnsafeUtility.SizeOf<FlowfieldCellGPU>(),
            ComputeBufferType.Structured
        );
        buffer.SetData(flowfieldGridGPU);
        Shader.SetGlobalBuffer("_FlowfieldCellBuffer", buffer);
    }

    /// <summary>
    /// EXPENSIVE
    /// </summary>
    private void RebuildGrid(ref SystemState state)
    {
        var nodes = TreeInsersionSystem.StaticBodiesAABBtree.nodes;
        var bodiesNum = nodes.Length;
        /// First, establish the size of the flow grid to encompase all static bodies
        float2 upperGrid = nodes[TreeInsersionSystem.StaticBodiesAABBtree.rootIndex].box.UpperBound;
        float2 lowerGrid = nodes[TreeInsersionSystem.StaticBodiesAABBtree.rootIndex].box.LowerBound;

        gridCenter = (upperGrid + lowerGrid)*0.5f;
        FlowfieldGridStorage.gridCenter = gridCenter;

        float2 dimentions = new float2((upperGrid.x - gridCenter.x) * 2, (upperGrid.y - gridCenter.y) * 2);
        /// Determine the cell resolution of the grid
        /// Expand the grid with one extra cell on the outer layer to allow cercling around the outer obstacles
        /// Expand the grid with a final  extra cell for the exterior agents to fallback to LOS
        ushort cellXresolution = (ushort)Mathf.CeilToInt(dimentions.x + 2 +2);
        ushort cellYresolution = (ushort)Mathf.CeilToInt(dimentions.y + 2 +2);
        dimentions = new float2(cellXresolution, cellYresolution);
        gridSize = new int2(cellXresolution, cellYresolution);
        FlowfieldGridStorage.gridSize = gridSize;
        Shader.SetGlobalVector("_FlowfieldPosSize", new Vector4(gridCenter.x, gridCenter.y, gridSize.x, gridSize.y));

        FlowfieldGridStorage.flowfieldGrid = new NativeArray<FlowfieldCellData>(gridSize.x* gridSize.y, Allocator.Persistent);
        flowfieldGrid = FlowfieldGridStorage.flowfieldGrid;
        flowfieldGridGPU = new NativeArray<FlowfieldCellGPU>(gridSize.x * gridSize.y, Allocator.Persistent);
        visitedCells = new NativeArray<bool>(flowfieldGrid.Length, Allocator.Persistent);

        
        var flowfieldE = FlowfieldQuery.GetSingletonEntity();
        state.EntityManager.SetComponentData<LocalTransform>(flowfieldE, new LocalTransform
        {
            Position = new float3(gridCenter.x, gridCenter.y, 100),
            Rotation = Quaternion.identity,
            Scale = 1
        });
        state.EntityManager.SetComponentData<PostTransformMatrix>(flowfieldE, new PostTransformMatrix
        {
            Value = float4x4.Scale(new float3(gridSize.x, gridSize.y, 1))
        });

        float cellsize = (dimentions.x / cellXresolution);
        cellNum = (ushort)(cellXresolution * cellYresolution);

        /// initialize each outter cell
        {
            int paddedGridCellNum = gridSize.x * gridSize.y;
            FlowfieldCellData newCell = new FlowfieldCellData();
            FlowfieldCellGPU newGPUcell = new FlowfieldCellGPU();
            newCell.InLineOfSight = true;
            newGPUcell.InLineOfSight = 1;
            for (int x = 1; x < gridSize.x; x++)
            {
                flowfieldGrid[x] = newCell;
                flowfieldGridGPU[x] = newGPUcell;
                flowfieldGrid[x+ (gridSize.y-1)* gridSize.x] = newCell;
                flowfieldGridGPU[x + (gridSize.y - 1) * gridSize.x] = newGPUcell;
                visitedCells[x] = true;
                visitedCells[x + (gridSize.y - 1) * gridSize.x] = true;
            }
            for (int y = 0; y < paddedGridCellNum; y+= gridSize.x)
            {
                flowfieldGrid[y] = newCell;
                flowfieldGridGPU[y] = newGPUcell;
                flowfieldGrid[y + gridSize.x-1] = newCell;
                flowfieldGridGPU[y + gridSize.x-1] = newGPUcell;
                visitedCells[y] = true;
                visitedCells[y + gridSize.x-1] = true;
            }
        }
     
        innerCellNum = (ushort)((cellXresolution-2) * (cellYresolution-2));
        /// Test each inner cells against static bodies
        /// EXPENSIVE
        for (int i = 0; i < innerCellNum; i++)
        {
            int xIdx = (i % (cellXresolution-2))+1;
            int yIdx = (i / (cellXresolution-2))+1;
            int flattenedIdx = xIdx + yIdx * gridSize.x;

            Vector2 cellAABBlowerBound = new Vector2((gridCenter.x - gridSize.x*0.5f) + (cellsize * xIdx), (gridCenter.y - gridSize.y * 0.5f) + (cellsize * yIdx));

            TreeInsersionSystem.DrawQuad(cellAABBlowerBound, new Vector2(cellAABBlowerBound.x + cellsize, cellAABBlowerBound.y + cellsize), Color.red);

            var cellOnObstacle = PhysicsCalls.IsShapeOverlaping(new float2(cellAABBlowerBound.x + cellsize*0.5f, cellAABBlowerBound.y + cellsize * 0.5f),
                0,
                new BoxShapeData { dimentions = new Vector2(cellsize, cellsize) },
                new AABB { LowerBound = cellAABBlowerBound, UpperBound = new Vector2(cellAABBlowerBound.x + cellsize, cellAABBlowerBound.y + cellsize) },
                PhysicsUtilities.CollisionLayer.StaticObstacleLayer,
                state.EntityManager);

            if(cellOnObstacle)
            {
                flowfieldGrid[flattenedIdx] = new FlowfieldCellData
                {
                    IsBlocked = true,
                    Direction = new float2(0.00001f, 0),
                    Cost = ushort.MaxValue,
                };
                flowfieldGridGPU[flattenedIdx] = new FlowfieldCellGPU
                {
                    IsBlocked = 1,
                    Cost = ushort.MaxValue,
                };
                /// Mark neighbor cells as IsNextToObstacle
                FlowfieldCellData newCell;
                ///Left
                newCell = flowfieldGrid[flattenedIdx - 1];
                newCell.IsNextToObstacle = true;
                flowfieldGrid[flattenedIdx - 1] = newCell;
                ///Up
                newCell = flowfieldGrid[flattenedIdx + gridSize.x];
                newCell.IsNextToObstacle = true;
                flowfieldGrid[flattenedIdx + gridSize.x] = newCell;
                ///Right
                newCell = flowfieldGrid[flattenedIdx + 1];
                newCell.IsNextToObstacle = true;
                flowfieldGrid[flattenedIdx + 1] = newCell;
                ///Bottom
                newCell = flowfieldGrid[flattenedIdx - gridSize.x];
                newCell.IsNextToObstacle = true;
                flowfieldGrid[flattenedIdx - gridSize.x] = newCell;

            }
            else
            {
                FlowfieldCellData newCell = flowfieldGrid[flattenedIdx];
                newCell.InLineOfSight = true;
                newCell.Direction = new float2(0.00001f,0);
                flowfieldGrid[flattenedIdx] = newCell;
                FlowfieldCellGPU newGPUcell = flowfieldGridGPU[flattenedIdx];
                newGPUcell.InLineOfSight = 1;
                flowfieldGridGPU[flattenedIdx] = newGPUcell;
            }
        }

    }

    private void UpdateGrid(ref SystemState state)
    {
        /// Clear every inner cells for updating
        innerCellNum = (ushort)((gridSize.x - 2) * (gridSize.y - 2));
        for (int i = 0; i < innerCellNum; i++)
        {
            int xIdx = (i % (gridSize.x-2)) + 1;
            int yIdx = (i / (gridSize.x-2)) + 1;
            int flattenedIdx = xIdx + yIdx * gridSize.x;

            FlowfieldCellData newCell = flowfieldGrid[flattenedIdx];
            FlowfieldCellGPU newGPUcell = flowfieldGridGPU[flattenedIdx];
            newCell.InLineOfSight = !newCell.IsBlocked;
            newGPUcell.InLineOfSight = 1 - newGPUcell.IsBlocked;
            newCell.Direction = new float2(0.00001f, 0);
            newGPUcell.Cost = ushort.MaxValue * newGPUcell.IsBlocked;
            newCell.Cost = (ushort)(ushort.MaxValue * newGPUcell.IsBlocked);
            flowfieldGrid[flattenedIdx] = newCell;
            flowfieldGridGPU[flattenedIdx] = newGPUcell;

            visitedCells[flattenedIdx] = false;
        }

        visibleCellsToVisit = new NativeQueue<int2>(Allocator.Temp);
        /// seperate cell check queue to visit after the visible one each iteration for the LOS to be properly assigned
        obstructedCellsToVisit = new NativeQueue<(int2, float2)>(Allocator.Temp);

        visitedCells[(int)(targetCell.x + targetCell.y * gridSize.x)] = true;

        visibleCellsToVisit.Enqueue(targetCell);

        currentEvaluatedCost = 0;
        visibleBatchSize = 1;
        obstructedBatchSize = 0;

        while ((!visibleCellsToVisit.IsEmpty() | !obstructedCellsToVisit.IsEmpty()))
        {
            for (int i = 0; i < visibleBatchSize; i++)
            {
                /// Assign the current cost to the current cells
                int2 evaluatedCellIdx = visibleCellsToVisit.Dequeue();
                int cellFlatenedIdx = (int)(evaluatedCellIdx.x + evaluatedCellIdx.y * gridSize.x);

                Vector2 directionToTarget = new float2((targetCell.x + 0.5f) - (evaluatedCellIdx.x + 0.5f), (targetCell.y + 0.5f) - (evaluatedCellIdx.y + 0.5f));
                directionToTarget = directionToTarget.normalized;

                var evaluatedCell = flowfieldGrid[cellFlatenedIdx];
                evaluatedCell.Cost = currentEvaluatedCost;
                evaluatedCell.Direction = directionToTarget;

                var evaluatedCellGPU = flowfieldGridGPU[cellFlatenedIdx];
                evaluatedCellGPU.Cost = currentEvaluatedCost;
                evaluatedCellGPU.ArrowRotation = -PhysicsUtilities.DirectionToRadians(directionToTarget, -Mathf.PI);

                flowfieldGrid[cellFlatenedIdx] = evaluatedCell;
                flowfieldGridGPU[cellFlatenedIdx] = evaluatedCellGPU;

                /// Propagate to the neighbors
                /// LEFT
                {
                    int2 leftCellIdx = new int2(evaluatedCellIdx.x - 1, evaluatedCellIdx.y); 
                    int leftCellFlatenedIdx = (int)(leftCellIdx.x + leftCellIdx.y * gridSize.x);
                    if (!visitedCells[leftCellFlatenedIdx])
                    {
                        if (!flowfieldGrid[leftCellFlatenedIdx].IsBlocked)
                        {
                            if (flowfieldGrid[leftCellFlatenedIdx].InLineOfSight)
                                visibleCellsToVisit.Enqueue(leftCellIdx);
                            else
                            {
                                //float2 leftFlowDirection = flowfieldGrid[leftCellFlatenedIdx].IsNextToObstacle ? new float2(1, 0) : new Vector2(1+ directionToTarget.x,0+ directionToTarget.y).normalized;
                                obstructedCellsToVisit.Enqueue((leftCellIdx, new float2(1, 0)));
                            }
                        }
                        else
                        {
                            /// Check if obstacle cell is a corner on both sides to flag the nessesary cells with "isobstructed"
                            /// HERE YOU WOULD THEORETICLY NEED TO MAKE SURE THAT THE IDX DONT GO OOB BUT 
                            /// BECAUSE MY GRID HAS A 1 CELL MARGIN AROUND OBSTACLES FOR ITS BOUNDS IT CANT HAPPEN FOR ME
                            if (!flowfieldGrid[leftCellFlatenedIdx + gridSize.x].IsBlocked)
                            {
                                RasterizeLOS(
                                    /// top right corner
                                    new float2(leftCellIdx.x + 1, leftCellIdx.y + 1),
                                    new float2(targetCell.x + 0.5f, targetCell.y + 0.5f)
                                    );
                            }
                            if (!flowfieldGrid[leftCellFlatenedIdx - gridSize.x].IsBlocked)
                            {
                                RasterizeLOS(
                                /// bottom right corner
                                new float2(leftCellIdx.x + 1, leftCellIdx.y),
                                new float2(targetCell.x + 0.5f, targetCell.y + 0.5f)
                                );
                            }
                            SetCellDirection(leftCellFlatenedIdx, new float2(1, 0), directionToTarget);
                        }
                        visitedCells[leftCellFlatenedIdx] = true;
                    }
                }
                /// UP
                {
                    int2 upCellIdx = new int2(evaluatedCellIdx.x,evaluatedCellIdx.y + 1);
                    int upCellFlatenedIdx = (int)(upCellIdx.x + upCellIdx.y * gridSize.x);

                    //if ((upCellIdx.y == 54) && (!visitedCells[upCellFlatenedIdx]))
                    //    Debug.Log("rerere");

                    if (!visitedCells[upCellFlatenedIdx])
                    {
                        if (!flowfieldGrid[upCellFlatenedIdx].IsBlocked)
                        {
                            if (flowfieldGrid[upCellFlatenedIdx].InLineOfSight)
                                visibleCellsToVisit.Enqueue(upCellIdx);
                            else
                            {
                                obstructedCellsToVisit.Enqueue((upCellIdx, new float2(0, -1)));
                            }
                        }
                        else
                        {
                            /// Check if obstacle cell is a corner on both sides to flag the nessesary cells with "isobstructed"
                            /// HERE YOU WOULD THEORETICLY NEED TO MAKE SURE THAT THE IDX DONT GO OOB BUT 
                            /// BECAUSE MY GRID HAS A 1 CELL MARGIN AROUND OBSTACLES FOR ITS BOUNDS IT CANT HAPPEN FOR ME
                            if (!flowfieldGrid[upCellFlatenedIdx + 1].IsBlocked)
                            {
                                RasterizeLOS(
                                    /// bottom right corner
                                    new float2(upCellIdx.x + 1, upCellIdx.y),
                                    new float2(targetCell.x + 0.5f, targetCell.y + 0.5f)
                                    );
                            }
                            if (!flowfieldGrid[upCellFlatenedIdx - 1].IsBlocked)
                            {
                                RasterizeLOS(
                                /// bottom left corner
                                new float2(upCellIdx.x, upCellIdx.y),
                                new float2(targetCell.x + 0.5f, targetCell.y + 0.5f)
                                );
                            }
                            SetCellDirection(upCellFlatenedIdx, new float2(0, -1), directionToTarget);
                        }
                        visitedCells[upCellFlatenedIdx] = true;
                    }
                }
                /// RIGHT
                {
                    int2 rightCellIdx = new int2(evaluatedCellIdx.x + 1, evaluatedCellIdx.y);
                    int rightCellFlatenedIdx = (int)(rightCellIdx.x + rightCellIdx.y * gridSize.x);
                    if (!visitedCells[rightCellFlatenedIdx])
                    {
                        if (!flowfieldGrid[rightCellFlatenedIdx].IsBlocked)
                        {
                            if (flowfieldGrid[rightCellFlatenedIdx].InLineOfSight)
                                visibleCellsToVisit.Enqueue(rightCellIdx);
                            else
                            {
                                obstructedCellsToVisit.Enqueue((rightCellIdx, new float2(-1, 0)));
                            }
                        }
                        else
                        {
                            /// Check if obstacle cell is a corner on both sides to flag the nessesary cells with "isobstructed"
                            /// HERE YOU WOULD THEORETICLY NEED TO MAKE SURE THAT THE IDX DONT GO OOB BUT 
                            /// BECAUSE MY GRID HAS A 1 CELL MARGIN AROUND OBSTACLES FOR ITS BOUNDS IT CANT HAPPEN FOR ME
                            if (!flowfieldGrid[rightCellFlatenedIdx + gridSize.x].IsBlocked)
                            {
                                RasterizeLOS(
                                    /// top left corner
                                    new float2(rightCellIdx.x, rightCellIdx.y + 1),
                                    new float2(targetCell.x + 0.5f, targetCell.y + 0.5f)
                                    );
                            }
                            if (!flowfieldGrid[rightCellFlatenedIdx - gridSize.x].IsBlocked)
                            {
                                RasterizeLOS(
                                /// bottom left corner
                                new float2(rightCellIdx.x, rightCellIdx.y),
                                new float2(targetCell.x + 0.5f, targetCell.y + 0.5f)
                                );
                            }
                            SetCellDirection(rightCellFlatenedIdx, new float2(-1, 0), directionToTarget);
                        }
                        visitedCells[rightCellFlatenedIdx] = true;
                    }
                }
                /// DOWN
                {
                    int2 downCellIdx = new int2(evaluatedCellIdx.x, evaluatedCellIdx.y - 1);
                    int downCellFlatenedIdx = (int)(downCellIdx.x + downCellIdx.y * gridSize.x);
                    if (!visitedCells[downCellFlatenedIdx])
                    {
                        if (!flowfieldGrid[downCellFlatenedIdx].IsBlocked)
                        {
                            if (flowfieldGrid[downCellFlatenedIdx].InLineOfSight)
                                visibleCellsToVisit.Enqueue(downCellIdx);
                            else
                            {
                                obstructedCellsToVisit.Enqueue((downCellIdx, new float2(0, 1)));
                            }
                        }
                        else
                        {
                            /// Check if obstacle cell is a corner on both sides to flag the nessesary cells with "isobstructed"
                            /// HERE YOU WOULD THEORETICLY NEED TO MAKE SURE THAT THE IDX DONT GO OOB BUT 
                            /// BECAUSE MY GRID HAS A 1 CELL MARGIN AROUND OBSTACLES FOR ITS BOUNDS IT CANT HAPPEN FOR ME
                            if (!flowfieldGrid[downCellFlatenedIdx + 1].IsBlocked)
                            {
                                RasterizeLOS(
                                    /// top right corner
                                    new float2(downCellIdx.x + 1, downCellIdx.y + 1),
                                    new float2(targetCell.x + 0.5f, targetCell.y + 0.5f)
                                    );
                            }
                            if (!flowfieldGrid[downCellFlatenedIdx - 1].IsBlocked)
                            {
                                RasterizeLOS(
                                /// top left corner
                                new float2(downCellIdx.x, downCellIdx.y + 1),
                                new float2(targetCell.x + 0.5f, targetCell.y + 0.5f)
                                );
                            }
                            SetCellDirection(downCellFlatenedIdx, new float2(0, 1), directionToTarget);
                        }
                        visitedCells[downCellFlatenedIdx] = true;
                    }
                }
            }
            for (int i = 0; i < obstructedBatchSize; i++)
            {
                /// Assign the current cost to the current cells
                (int2 evaluatedCellIdx, float2 forcedDirection) = obstructedCellsToVisit.Dequeue();
                int cellFlatenedIdx = (int)(evaluatedCellIdx.x + evaluatedCellIdx.y * gridSize.x);

                var evaluatedCell = flowfieldGrid[cellFlatenedIdx];
                var evaluatedCellGPU = flowfieldGridGPU[cellFlatenedIdx];

                evaluatedCell.Cost = currentEvaluatedCost;
                evaluatedCellGPU.Cost = currentEvaluatedCost;
                evaluatedCell.InLineOfSight = false;
                evaluatedCellGPU.InLineOfSight = 0;

                int2 leftCellIdx = new int2(evaluatedCellIdx.x - 1, evaluatedCellIdx.y);
                int leftCellFlatenedIdx = (int)(leftCellIdx.x + leftCellIdx.y * gridSize.x);

                int2 upCellIdx = new int2(evaluatedCellIdx.x, evaluatedCellIdx.y + 1);
                int upCellFlatenedIdx = (int)(upCellIdx.x + upCellIdx.y * gridSize.x);

                int2 rightCellIdx = new int2(evaluatedCellIdx.x + 1, evaluatedCellIdx.y);
                int rightCellFlatenedIdx = (int)(rightCellIdx.x + rightCellIdx.y * gridSize.x);

                int2 downCellIdx = new int2(evaluatedCellIdx.x, evaluatedCellIdx.y - 1);
                int downCellFlatenedIdx = (int)(downCellIdx.x + downCellIdx.y * gridSize.x);

                Vector2 proximityDirectionInfluance = (flowfieldGrid[leftCellFlatenedIdx].Direction
                    + flowfieldGrid[upCellFlatenedIdx].Direction
                    + flowfieldGrid[rightCellFlatenedIdx].Direction
                    + flowfieldGrid[downCellFlatenedIdx].Direction);
                proximityDirectionInfluance = proximityDirectionInfluance.normalized;

                Vector2 cellDirection = (evaluatedCell.IsNextToObstacle ? proximityDirectionInfluance * 0.2f + (Vector2)forcedDirection * 0.8f : proximityDirectionInfluance * 0.8f + (Vector2)forcedDirection * 0.2f);
        
                /// Propagate to the neighbors
                /// LEFT
                {
                    float distanceFromTarget = new Vector2(leftCellIdx.x - targetCell.x, leftCellIdx.y - targetCell.y).magnitude;
                    if (!visitedCells[leftCellFlatenedIdx])
                    {
                        if (!flowfieldGrid[leftCellFlatenedIdx].IsBlocked)
                        {
                            obstructedCellsToVisit.Enqueue((leftCellIdx, new float2(1, 0)));
                        }
                        else
                        {
                            SetCellDirection(leftCellFlatenedIdx, new float2(1, 0), cellDirection);
                        }
                        visitedCells[leftCellFlatenedIdx] = true;
                    }
                }
                /// UP
                {
                    float distanceFromTarget = new Vector2(upCellIdx.x - targetCell.x, upCellIdx.y - targetCell.y).magnitude;
                    if (!visitedCells[upCellFlatenedIdx])
                    {
                        if (!flowfieldGrid[upCellFlatenedIdx].IsBlocked)
                        {
                            obstructedCellsToVisit.Enqueue((upCellIdx, new float2(0, -1)));
                        }
                        else
                        {
                            SetCellDirection(upCellFlatenedIdx, new float2(0, -1), cellDirection);
                        }
                        visitedCells[upCellFlatenedIdx] = true;
                    }
                }
                /// RIGHT
                {
                    float distanceFromTarget = new Vector2(rightCellIdx.x - targetCell.x, rightCellIdx.y - targetCell.y).magnitude;
                    if (!visitedCells[rightCellFlatenedIdx])
                    {
                        if (!flowfieldGrid[rightCellFlatenedIdx].IsBlocked)
                        {
                            obstructedCellsToVisit.Enqueue((rightCellIdx, new float2(-1, 0)));
                        }
                        else
                        {
                            SetCellDirection(rightCellFlatenedIdx, new float2(-1, 0), cellDirection);
                        }
                        visitedCells[rightCellFlatenedIdx] = true;
                    }
                }
                /// DOWN
                {
                    float distanceFromTarget = new Vector2(downCellIdx.x - targetCell.x, downCellIdx.y - targetCell.y).magnitude;
                    if (!visitedCells[downCellFlatenedIdx])
                    {
                        if (!flowfieldGrid[downCellFlatenedIdx].IsBlocked)
                        {
                            obstructedCellsToVisit.Enqueue((downCellIdx, new float2(0, 1)));
                        }
                        else
                        {
                            SetCellDirection(downCellFlatenedIdx, new float2(0, 1), cellDirection);
                        }
                        visitedCells[downCellFlatenedIdx] = true;
                    }
                }



                evaluatedCell.Direction = cellDirection;

                evaluatedCellGPU.ArrowRotation = -PhysicsUtilities.DirectionToRadians(cellDirection, -Mathf.PI);

                flowfieldGrid[cellFlatenedIdx] = evaluatedCell;
                flowfieldGridGPU[cellFlatenedIdx] = evaluatedCellGPU;

            }

            visibleBatchSize = (ushort)visibleCellsToVisit.Count;
            obstructedBatchSize = (ushort)obstructedCellsToVisit.Count;
            currentEvaluatedCost++;
        }

        ComputeBuffer buffer = new ComputeBuffer(
            flowfieldGridGPU.Length,
            UnsafeUtility.SizeOf<FlowfieldCellGPU>(),
            ComputeBufferType.Structured
        );
        buffer.SetData(flowfieldGridGPU);
        Shader.SetGlobalBuffer("_FlowfieldCellBuffer", buffer);

    }

    /*private void TEMPupdateStep()
    {

        for (int i = 0; i < batchSize; i++)
        {
            /// Assign the current cost to the current cells
            int2 evaluatedCellIdx = cellsToVisit.Dequeue();
            int cellFlatenedIdx = (int)(evaluatedCellIdx.x + evaluatedCellIdx.y * gridSize.x);
            var evaluatedCell = flowfieldGrid[cellFlatenedIdx];
            evaluatedCell.Cost = currentEvaluatedCost;
            flowfieldGrid[cellFlatenedIdx] = evaluatedCell;
            var evaluatedCellGPU = flowfieldGridGPU[cellFlatenedIdx];
            evaluatedCellGPU.Cost = currentEvaluatedCost;
            flowfieldGridGPU[cellFlatenedIdx] = evaluatedCellGPU;
            /// Propagate to the neighbors
            /// LEFT
            {
                int2 leftCellIdx = new int2((int)Mathf.Max(evaluatedCellIdx.x - 1, 0), evaluatedCellIdx.y); /// prevent to loop across -> reevaluate the same
                int leftCellFlatenedIdx = (int)(leftCellIdx.x + leftCellIdx.y * gridSize.x);
                bool validCell = (!flowfieldGrid[leftCellFlatenedIdx].IsBlocked) && (!visitedCells[leftCellFlatenedIdx]);
                if (validCell)
                {
                    cellsToVisit.Enqueue(leftCellIdx);
                    visitedCells[leftCellFlatenedIdx] = true;
                }
            }
            /// UP
            {
                int2 upCellIdx = new int2(evaluatedCellIdx.x, (int)Mathf.Min(evaluatedCellIdx.y + 1, gridSize.y - 1)); /// prevent to loop across -> reevaluate the same
                int upCellFlatenedIdx = (int)(upCellIdx.x + upCellIdx.y * gridSize.x);
                bool validCell = (!flowfieldGrid[upCellFlatenedIdx].IsBlocked) && (!visitedCells[upCellFlatenedIdx]);
                if (validCell)
                {
                    cellsToVisit.Enqueue(upCellIdx);
                    visitedCells[upCellFlatenedIdx] = true;
                }
            }
            /// RIGHT
            {
                int2 rightCellIdx = new int2((int)Mathf.Min(evaluatedCellIdx.x + 1, gridSize.x - 1), evaluatedCellIdx.y); /// prevent to loop across -> reevaluate the same
                int rightCellFlatenedIdx = (int)(rightCellIdx.x + rightCellIdx.y * gridSize.x);
                bool validCell = (!flowfieldGrid[rightCellFlatenedIdx].IsBlocked) && (!visitedCells[rightCellFlatenedIdx]);
                if (validCell)
                {
                    cellsToVisit.Enqueue(rightCellIdx);
                    visitedCells[rightCellFlatenedIdx] = true;
                }
            }
            /// DOWN
            {
                int2 downCellIdx = new int2(evaluatedCellIdx.x, (int)Mathf.Max(evaluatedCellIdx.y - 1, 0)); /// prevent to loop across -> reevaluate the same
                int downCellFlatenedIdx = (int)(downCellIdx.x + downCellIdx.y * gridSize.x);
                bool validCell = (!flowfieldGrid[downCellFlatenedIdx].IsBlocked) && (!visitedCells[downCellFlatenedIdx]);
                if (validCell)
                {
                    cellsToVisit.Enqueue(downCellIdx);
                    visitedCells[downCellFlatenedIdx] = true;
                }
            }
        }
        batchSize = (ushort)cellsToVisit.Count;
        currentEvaluatedCost++;

        ComputeBuffer buffer = new ComputeBuffer(
            flowfieldGridGPU.Length,
            UnsafeUtility.SizeOf<FlowfieldCellGPU>(),
            ComputeBufferType.Structured
        );
        buffer.SetData(flowfieldGridGPU);
        Shader.SetGlobalBuffer("_FlowfieldCellBuffer", buffer);
    }
    */


    private void RasterizeLOS(Vector2 cornerStart,Vector2 targetCellCenter)
    {

        Vector2 cornerFromTraget = (cornerStart - targetCellCenter);
        float2 relDirection = new Vector2(Mathf.Abs(cornerFromTraget.x), Mathf.Abs(cornerFromTraget.y)) .normalized;
        Vector2 directionSign = new Vector2(Mathf.Sign(cornerFromTraget.x), Mathf.Sign(cornerFromTraget.y));
        float slope = relDirection.x;

        Vector2 delta = cornerFromTraget.normalized;
      
        Vector2 evaluatedPoint = cornerStart + delta * 0.5f;

        /// compare the TargetToCorner ray to the TargetToGridCorner ray inclinason to know if
        /// the corner ray bumps vertically or horizontally and define cell iteration num that way
        Vector2 relevantGridCorner = new Vector2(math.select(1, (gridSize.x-1), cornerFromTraget.x > 0), math.select(1, (gridSize.y-1), cornerFromTraget.y > 0));
     
        float slopeToGridCorner = Mathf.Abs(new Vector2(targetCellCenter.x - relevantGridCorner.x, targetCellCenter.y - relevantGridCorner.y).normalized.x);

        float relevantLimit = math.select(relevantGridCorner.y, relevantGridCorner.x, slope > slopeToGridCorner);

        float distanceToLimit = Mathf.Abs(relevantLimit - (math.select(evaluatedPoint.y, evaluatedPoint.x, slope > slopeToGridCorner)));

        int cellsToGridEnd = Mathf.FloorToInt(distanceToLimit / (math.select(Mathf.Abs(delta.y), Mathf.Abs(delta.x), slope > slopeToGridCorner))) +1;

        int2 evaluatedCoords = new int2(Mathf.FloorToInt(evaluatedPoint.x), Mathf.FloorToInt(evaluatedPoint.y));

        /// Remaining iteration
        while (cellsToGridEnd>0)
        ///while (evaluatedCoords.x < gridSize.x && evaluatedCoords.y< gridSize.y && evaluatedCoords.x >= 0 && evaluatedCoords.y >= 0) SAFETY
        {
            /// testing
            //var testcornerStart = cornerStart - (Vector2)((float2)gridSize * 0.5f - gridCenter);
            //var testevaluatedPoint = evaluatedPoint - (Vector2)((float2)gridSize * 0.5f - gridCenter);
            //var testerelevantGridCorner = (float2)relevantGridCorner - ((float2)gridSize * 0.5f - gridCenter);
            //Debug.DrawLine(new Vector3(testcornerStart.x, testcornerStart.y, 0), new Vector3(testevaluatedPoint.x, testevaluatedPoint.y, 0), slope > slopeToGridCorner ? Color.green : Color.red,1);

            int flattenedIdx = evaluatedCoords.x + evaluatedCoords.y * gridSize.x;
            var newFlowfieldCell = flowfieldGrid[flattenedIdx];
            var newFlowfieldCellGPU = flowfieldGridGPU[flattenedIdx];

            newFlowfieldCell.InLineOfSight = false;
            newFlowfieldCellGPU.InLineOfSight = 0;
            flowfieldGrid[flattenedIdx] = newFlowfieldCell;
            flowfieldGridGPU[flattenedIdx] = newFlowfieldCellGPU;

            evaluatedPoint += delta;
            evaluatedCoords = new int2(Mathf.FloorToInt(evaluatedPoint.x), Mathf.FloorToInt(evaluatedPoint.y));
            cellsToGridEnd--;
        }

    }

    void SetCellDirection(int flatenedIdx, Vector2 direction,Vector2 influanceDirection)
    {
        var newObstructedCell = flowfieldGrid[flatenedIdx];
        var newObstructedCellGPU = flowfieldGridGPU[flatenedIdx];
        direction = (direction + influanceDirection).normalized;
        newObstructedCell.Direction = direction;
        newObstructedCellGPU.ArrowRotation = -PhysicsUtilities.DirectionToRadians(direction, -Mathf.PI);
        flowfieldGridGPU[flatenedIdx] = newObstructedCellGPU;
        flowfieldGrid[flatenedIdx] = newObstructedCell;
    }


}