using UnityEngine;

namespace UI
{
    public class PlayerCharacterUI : MonoBehaviour
    {
        [SerializeField] private QuickSlot[] quickSlots;

        public void SetQuickSlotData(int index, Sprite icon, float cooldown)
        {
            if (index > quickSlots.Length)
            {
                return;
            }
            quickSlots[index].SetQuickSlotData(icon, cooldown);
        }
    
        public void UseSkill(int index)
        {
            quickSlots[index].UseSkill();
        }

        public void SelectSkill(int index, bool selected)
        {
            if (index > 0)
            {
                for (int i = 1; i < quickSlots.Length; i++)
                {
                    quickSlots[i].SelectSkill(false);
                }
                quickSlots[index].SelectSkill(selected);
            }
        }
    
    }
}
