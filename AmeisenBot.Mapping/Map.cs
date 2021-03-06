﻿using AmeisenBotMapping.objects;
using System.Collections.Generic;

namespace AmeisenBotMapping
{
    public class Map
    {
        public List<MapNode> Nodes { get; private set; }

        public Map(MapNode initialNode)
        {
            Nodes = new List<MapNode> { initialNode };
        }

        public Map(List<MapNode> initialNodes)
        {
            Nodes = initialNodes;
        }
    }
}