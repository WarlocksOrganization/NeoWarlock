using DataSystem.Database;
using UnityEngine;

namespace GameManagement
{
    public class GameManager : MonoBehaviour
    {
        #region Singleton
        private static GameManager instance;

        public static GameManager Instance
        {
            get
            {
                if (instance == null)
                {
                    // 새로운 GameObject를 생성하고 GameManager 컴포넌트를 추가
                    GameObject go = new GameObject("GameManager");
                    instance = go.AddComponent<GameManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        #endregion

        void Start()
        {
            Database.LoadDataBase();
        }
    }
}