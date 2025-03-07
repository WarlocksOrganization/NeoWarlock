using Mirror;
using UnityEngine;

namespace GameManagement
{
    public class SpawnPosition : MonoBehaviour
    {
        [SerializeField] private Transform[] position;

        private int index;

        [Server]
        public Vector3 GetSpawnPosition()
        {
            Vector3 pos = position[index++ % position.Length].position;
            return pos;
        }
    }
}