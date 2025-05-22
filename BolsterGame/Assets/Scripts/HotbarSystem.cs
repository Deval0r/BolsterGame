using UnityEngine;
using System.Collections.Generic;

public class HotbarSystem : MonoBehaviour
{
    [System.Serializable]
    public class HotbarItem
    {
        public string itemName;
        public GameObject itemPrefab;
        public KeyCode hotkey;
    }

    [Header("Hotbar Settings")]
    [SerializeField] private List<HotbarItem> hotbarItems = new List<HotbarItem>();
    [SerializeField] private int currentSelectedIndex = 0;

    private GameObject currentHeldItem;
    
    private void Update()
    {
        // Check for number keys 1-9
        for (int i = 0; i < hotbarItems.Count; i++)
        {
            if (Input.GetKeyDown(hotbarItems[i].hotkey))
            {
                SelectItem(i);
            }
        }

        // Check for scroll wheel
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            int newIndex = currentSelectedIndex + (scroll > 0 ? -1 : 1);
            if (newIndex < 0) newIndex = hotbarItems.Count - 1;
            if (newIndex >= hotbarItems.Count) newIndex = 0;
            SelectItem(newIndex);
        }
    }

    private void SelectItem(int index)
    {
        currentSelectedIndex = index;
        
        // Destroy current held item
        if (currentHeldItem != null)
        {
            Destroy(currentHeldItem);
        }

        // Instantiate new item
        if (hotbarItems[index].itemPrefab != null)
        {
            currentHeldItem = Instantiate(hotbarItems[index].itemPrefab, transform);
        }
    }

    public bool IsHoldingHammer()
    {
        return currentSelectedIndex >= 0 && 
               currentSelectedIndex < hotbarItems.Count && 
               hotbarItems[currentSelectedIndex].itemName == "Hammer";
    }
} 