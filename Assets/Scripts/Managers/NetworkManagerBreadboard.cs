using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mirror
{
    public class NetworkManagerBreadboard : NetworkManager
    {
        [SerializeField] private Transform[] playerSpawn;
        [SerializeField] private Transform[] breadboardSpawn;

        // Track which breadboard spots are occupied
        private bool[] breadboardSpotOccupied;
        // Dictionary to track which player owns which breadboard
        private Dictionary<uint, GameObject> playerBreadboards = new Dictionary<uint, GameObject>();

        public static event Action OnClientConnected;
        public static event Action OnClientDisconnected;

        // Add this method to get a player's breadboard
        public GameObject GetPlayerBreadboard(uint netId)
        {
            playerBreadboards.TryGetValue(netId, out GameObject breadboard);
            return breadboard;
        }

        // Add this method to get a player's breadboard by PlayerController
        public GameObject GetPlayerBreadboard(PlayerController player)
        {
            if (player == null || player.netIdentity == null) return null;
            return GetPlayerBreadboard(player.netIdentity.netId);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            breadboardSpotOccupied = new bool[breadboardSpawn.Length];
        }

        // Start with instructor role for first player, student for others
        private GameManager.UserRole GetRoleForNextPlayer()
        {
            return numPlayers == 0 ?
                GameManager.UserRole.Instructor :
                GameManager.UserRole.Student;
        }

        // Find the first available breadboard spawn point
        private int FindAvailableBreadboardSpot()
        {
            for (int i = 0; i < breadboardSpotOccupied.Length; i++)
            {
                if (!breadboardSpotOccupied[i])
                {
                    return i;
                }
            }
            // If no spots available, use the last one (or add error handling)
            return breadboardSpotOccupied.Length - 1;
        }

        public override void OnServerAddPlayer(NetworkConnection conn)
        {
            // Determine role for this player
            GameManager.UserRole role = GetRoleForNextPlayer();

            // Spawn player at appropriate position
            Transform spawnPoint = role == GameManager.UserRole.Instructor ?
                GameManager.Instance.instructorPos :
                playerSpawn[numPlayers];

            GameObject player = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);

            // Add spectator controller to instructor players (only if not Android)
#if !UNITY_ANDROID
            if (role == GameManager.UserRole.Instructor)
            {
                player.AddComponent<InstructorSpectatorController>();
            }
#endif

            NetworkServer.AddPlayerForConnection(conn, player);

            // If student, also spawn a breadboard
#if UNITY_EDITOR
            if (true)
#else
            if (role == GameManager.UserRole.Student)
#endif
            {
                int spawnIndex = FindAvailableBreadboardSpot();
                breadboardSpotOccupied[spawnIndex] = true;

                Debug.Log("Spawned breadboard for: " + conn.identity.netId + " at spot " + spawnIndex);
                GameObject breadboard = Instantiate(spawnPrefabs.Find(prefab => prefab.name == "Breadboard"));
                NetworkServer.Spawn(breadboard, conn);
                Debug.Log("POSITION BEFORE CHAANNGE:" + breadboard.transform.position);
                breadboard.transform.position = breadboardSpawn[spawnIndex].transform.position;

                BreadboardController controller = breadboard.GetComponent<BreadboardController>();

                // Track this breadboard for cleanup on disconnect
                playerBreadboards[conn.identity.netId] = breadboard;
            }
        }

        public override void OnServerDisconnect(NetworkConnection conn)
        {
            // Clean up student scores when they disconnect
            if (conn.identity != null)
            {
                PlayerController player = conn.identity.GetComponent<PlayerController>();
                if (player != null && player.id > 0)
                {
                    ClassroomManager scoreManager = GameObject.Find("chalkboard").GetComponent<ClassroomManager>();
                    if (scoreManager != null)
                    {
                        scoreManager.CmdRemoveStudent(player.id);
                    }
                }
            }

            // Keep your existing breadboard cleanup code
            if (conn.identity != null && playerBreadboards.TryGetValue(conn.identity.netId, out GameObject breadboard))
            {
                // Find the index of this breadboard to mark the spot as available
                for (int i = 0; i < breadboardSpawn.Length; i++)
                {
                    if (Vector3.Distance(breadboard.transform.position, breadboardSpawn[i].position) < 0.1f)
                    {
                        breadboardSpotOccupied[i] = false;
                        break;
                    }
                }

                // Destroy the breadboard
                NetworkServer.Destroy(breadboard);
                playerBreadboards.Remove(conn.identity.netId);
            }

            base.OnServerDisconnect(conn);
        }

        public override void OnClientConnect(NetworkConnection conn)
        {
            base.OnClientConnect(conn);
            OnClientConnected?.Invoke();
        }

        public override void OnClientDisconnect(NetworkConnection conn)
        {
            base.OnClientDisconnect(conn);
            OnClientDisconnected?.Invoke();
        }
    }
}
