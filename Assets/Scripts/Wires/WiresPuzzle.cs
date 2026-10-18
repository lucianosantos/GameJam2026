using System;
using System.Collections.Generic;

namespace GameJam2026.Wires
{
    [Serializable]
    public struct WiresPair
    {
        public string first;
        public string second;
        public WiresPair(string first, string second) { this.first = first; this.second = second; }
    }

    // Pure puzzle state: physical sockets are distinct, cables have no identity in the solution.
    public sealed class WiresPuzzle
    {
        private readonly string[] ports;
        private readonly WiresPair[] solution;
        private readonly int[] connections;
        public bool IsCompleted { get; private set; }
        public int EndpointCount => connections.Length;

        public WiresPuzzle(string[] socketPorts, WiresPair[] correctPairs)
        {
            if (socketPorts == null || correctPairs == null || correctPairs.Length != 4 || socketPorts.Length != 8)
                throw new ArgumentException("Configure eight physical sockets and four solution pairs.");
            ports = (string[])socketPorts.Clone();
            solution = (WiresPair[])correctPairs.Clone();
            var capacity = new Dictionary<string, int>();
            foreach (string port in ports)
            {
                if (string.IsNullOrWhiteSpace(port)) throw new ArgumentException("Port IDs cannot be empty.");
                capacity[port] = capacity.TryGetValue(port, out int count) ? count + 1 : 1;
            }
            var demand = new Dictionary<string, int>();
            foreach (var pair in solution)
            {
                if (string.IsNullOrWhiteSpace(pair.first) || string.IsNullOrWhiteSpace(pair.second))
                    throw new ArgumentException("Solution ports cannot be empty.");
                foreach (string port in new[] { pair.first, pair.second })
                    demand[port] = demand.TryGetValue(port, out int count) ? count + 1 : 1;
            }
            if (demand.Count != capacity.Count) throw new ArgumentException("Solution must use the configured sockets.");
            foreach (var item in demand)
                if (!capacity.TryGetValue(item.Key, out int count) || count != item.Value)
                    throw new ArgumentException("Solution exceeds socket capacity.");
            connections = new int[8];
            for (int i = 0; i < connections.Length; i++) connections[i] = -1;
        }

        public int GetSocket(int endpoint) => ValidEndpoint(endpoint) ? connections[endpoint] : -1;
        public bool IsOccupied(int socket) => Array.IndexOf(connections, socket) >= 0 && socket >= 0;
        private bool ValidEndpoint(int endpoint) => endpoint >= 0 && endpoint < connections.Length;

        public bool Disconnect(int endpoint)
        {
            if (IsCompleted || !ValidEndpoint(endpoint)) return false;
            connections[endpoint] = -1;
            return true;
        }

        public bool TryConnect(int endpoint, int socket)
        {
            if (IsCompleted || !ValidEndpoint(endpoint) || socket < 0 || socket >= ports.Length ||
                connections[endpoint] != -1 || IsOccupied(socket)) return false;
            connections[endpoint] = socket;
            IsCompleted = MatchesSolution();
            return true;
        }

        private bool MatchesSolution()
        {
            foreach (int socket in connections) if (socket < 0) return false;
            var matched = new bool[solution.Length];
            for (int cable = 0; cable < solution.Length; cable++)
            {
                string a = ports[connections[cable * 2]], b = ports[connections[cable * 2 + 1]];
                bool found = false;
                for (int i = 0; i < solution.Length; i++)
                    if (!matched[i] && ((a == solution[i].first && b == solution[i].second) ||
                                        (a == solution[i].second && b == solution[i].first)))
                    { matched[i] = true; found = true; break; }
                if (!found) return false;
            }
            return true;
        }
    }
}
