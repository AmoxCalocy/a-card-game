using System;
using System.Collections.Generic;
using UnityEngine;

namespace OneManJourney.Runtime
{
    public static class JourneyMapGenerator
    {
        public static JourneyMap Generate(int seed, JourneyMapGenerationConfig config)
        {
            config ??= new JourneyMapGenerationConfig();
            var random = new System.Random(seed);
            var mutableNodes = BuildNodes(config, random);
            LinkNodes(mutableNodes, config, random);
            int routeCount = CountRoutes(mutableNodes);
            int branchingNodes = CountBranchingNodes(mutableNodes);

            var nodes = new List<JourneyMapNode>(mutableNodes.Count);
            for (int i = 0; i < mutableNodes.Count; i++)
            {
                MutableNode source = mutableNodes[i];
                nodes.Add(new JourneyMapNode(source.Id, source.LayerIndex, source.LaneIndex, source.NodeType, source.NextNodeIds));
            }

            return new JourneyMap(seed, nodes, routeCount, branchingNodes);
        }

        private static List<MutableNode> BuildNodes(JourneyMapGenerationConfig config, System.Random random)
        {
            int layerCount = config.LayerCount;
            int lanesPerLayer = config.LanesPerLayer;
            int nextNodeId = 0;
            var nodes = new List<MutableNode>();

            nodes.Add(new MutableNode(nextNodeId++, 0, 0, JourneyNodeType.Battle));

            for (int layer = 1; layer < layerCount - 1; layer++)
            {
                for (int lane = 0; lane < lanesPerLayer; lane++)
                {
                    JourneyNodeType nodeType = config.RollNodeType(random);
                    nodes.Add(new MutableNode(nextNodeId++, layer, lane, nodeType));
                }
            }

            nodes.Add(new MutableNode(nextNodeId, layerCount - 1, 0, JourneyNodeType.Boss));
            return nodes;
        }

        private static void LinkNodes(List<MutableNode> nodes, JourneyMapGenerationConfig config, System.Random random)
        {
            var nodesByLayer = GroupByLayer(nodes, config.LayerCount);
            if (nodesByLayer.Count < 2)
            {
                return;
            }

            IReadOnlyList<MutableNode> firstContentLayer = nodesByLayer[1];
            MutableNode startNode = nodesByLayer[0][0];
            HashSet<int> startTargets = PickDistinctLanes(random, firstContentLayer.Count, Math.Min(2, firstContentLayer.Count));
            foreach (int lane in startTargets)
            {
                startNode.NextNodeIds.Add(firstContentLayer[lane].Id);
            }

            for (int layer = 1; layer < nodesByLayer.Count - 2; layer++)
            {
                IReadOnlyList<MutableNode> currentLayer = nodesByLayer[layer];
                IReadOnlyList<MutableNode> nextLayer = nodesByLayer[layer + 1];
                for (int i = 0; i < currentLayer.Count; i++)
                {
                    MutableNode node = currentLayer[i];
                    ConnectToNextLayer(node, nextLayer, random);
                }
            }

            IReadOnlyList<MutableNode> beforeBossLayer = nodesByLayer[nodesByLayer.Count - 2];
            MutableNode bossNode = nodesByLayer[nodesByLayer.Count - 1][0];
            for (int i = 0; i < beforeBossLayer.Count; i++)
            {
                beforeBossLayer[i].NextNodeIds.Add(bossNode.Id);
            }
        }

        private static List<List<MutableNode>> GroupByLayer(IReadOnlyList<MutableNode> nodes, int layerCount)
        {
            var result = new List<List<MutableNode>>(layerCount);
            for (int layer = 0; layer < layerCount; layer++)
            {
                result.Add(new List<MutableNode>());
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                MutableNode node = nodes[i];
                result[node.LayerIndex].Add(node);
            }

            return result;
        }

        private static void ConnectToNextLayer(MutableNode node, IReadOnlyList<MutableNode> nextLayer, System.Random random)
        {
            var lanes = new HashSet<int>();
            int clampedLane = Mathf.Clamp(node.LaneIndex, 0, nextLayer.Count - 1);
            lanes.Add(clampedLane);

            if (nextLayer.Count > 1)
            {
                int offset = random.NextDouble() < 0.5d ? -1 : 1;
                int branchLane = Mathf.Clamp(node.LaneIndex + offset, 0, nextLayer.Count - 1);
                lanes.Add(branchLane);

                if (lanes.Count < 2)
                {
                    lanes.Add(random.Next(0, nextLayer.Count));
                }
            }

            foreach (int lane in lanes)
            {
                node.NextNodeIds.Add(nextLayer[lane].Id);
            }
        }

        private static HashSet<int> PickDistinctLanes(System.Random random, int laneCount, int pickCount)
        {
            var lanes = new HashSet<int>();
            if (laneCount <= 0)
            {
                return lanes;
            }

            while (lanes.Count < pickCount)
            {
                lanes.Add(random.Next(0, laneCount));
            }

            return lanes;
        }

        private static int CountRoutes(IReadOnlyList<MutableNode> nodes)
        {
            if (nodes.Count == 0)
            {
                return 0;
            }

            var routeCounts = new Dictionary<int, long>(nodes.Count);
            for (int i = nodes.Count - 1; i >= 0; i--)
            {
                MutableNode node = nodes[i];
                if (node.NextNodeIds.Count == 0)
                {
                    routeCounts[node.Id] = 1;
                    continue;
                }

                long sum = 0;
                for (int j = 0; j < node.NextNodeIds.Count; j++)
                {
                    int nextId = node.NextNodeIds[j];
                    if (routeCounts.TryGetValue(nextId, out long nextCount))
                    {
                        sum += nextCount;
                    }
                }

                routeCounts[node.Id] = sum;
            }

            long routeCount = routeCounts.TryGetValue(nodes[0].Id, out long count) ? count : 0;
            return routeCount > int.MaxValue ? int.MaxValue : (int)routeCount;
        }

        private static int CountBranchingNodes(IReadOnlyList<MutableNode> nodes)
        {
            int count = 0;
            for (int i = 0; i < nodes.Count; i++)
            {
                if (nodes[i].NextNodeIds.Count > 1)
                {
                    count++;
                }
            }

            return count;
        }

        private sealed class MutableNode
        {
            public MutableNode(int id, int layerIndex, int laneIndex, JourneyNodeType nodeType)
            {
                Id = id;
                LayerIndex = layerIndex;
                LaneIndex = laneIndex;
                NodeType = nodeType;
                NextNodeIds = new List<int>();
            }

            public int Id { get; }
            public int LayerIndex { get; }
            public int LaneIndex { get; }
            public JourneyNodeType NodeType { get; }
            public List<int> NextNodeIds { get; }
        }
    }
}
