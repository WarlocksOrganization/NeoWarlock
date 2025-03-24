using Mirror;
using UnityEngine;

namespace GameManagement
{
    public class SpawnPosition : MonoBehaviour
    {
        [SerializeField] private Transform[] position;
        private int index;

        private void Awake()
        {
            ShufflePositions();
        }

        [Server]
        public Vector3 GetSpawnPosition()
        {
            if (position == null || position.Length == 0)
            {
                Debug.LogWarning("[SpawnPosition] 스폰 위치가 없습니다.");
                return Vector3.zero;
            }

            Vector3 pos = position[index++ % position.Length].position;
            return pos;
        }

        private void ShufflePositions()
        {
            for (int i = 0; i < position.Length; i++)
            {
                int rand = Random.Range(i, position.Length);
                (position[i], position[rand]) = (position[rand], position[i]);
            }
        }
    }
}