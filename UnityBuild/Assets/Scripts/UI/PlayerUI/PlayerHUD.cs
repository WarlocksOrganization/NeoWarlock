using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class PlayerHUD : MonoBehaviour
    {
        [SerializeField] private Slider HpSlider;

        void Update()
        {
            transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward, Camera.main.transform.rotation * Vector3.up);
        }

        public void SetHpBar(float hp)
        {
            HpSlider.value = hp;
        }
    }
}
