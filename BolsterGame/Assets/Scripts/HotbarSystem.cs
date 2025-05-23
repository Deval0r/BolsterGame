using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class HotbarSystem : MonoBehaviour
{
    [System.Serializable]
    public class HotbarItem
    {
        public string itemName;
        public GameObject itemPrefab;
        public Image slotBackground;
        public Image itemIcon;
    }

    [Header("Hotbar Settings")]
    [SerializeField] private List<HotbarItem> hotbarItems = new List<HotbarItem>();
    [SerializeField] private int currentSelectedIndex = 0;
    [SerializeField] private Color selectedSlotColor = new Color(1f, 1f, 1f, 0.5f);
    [SerializeField] private Color normalSlotColor = new Color(1f, 1f, 1f, 0.2f);

    private GameObject currentHeldItem;
    
    private void Start()
    {
        // Ensure hand is first and hammer is second
        SortHotbarItems();
        UpdateSlotHighlighting();
    }

    private void SortHotbarItems()
    {
        // Find hand and hammer items
        HotbarItem handItem = null;
        HotbarItem hammerItem = null;
        List<HotbarItem> otherItems = new List<HotbarItem>();

        foreach (var item in hotbarItems)
        {
            if (item.itemName == "Hand")
                handItem = item;
            else if (item.itemName == "Hammer")
                hammerItem = item;
            else
                otherItems.Add(item);
        }

        // Create new sorted list
        List<HotbarItem> sortedItems = new List<HotbarItem>();
        
        // Add hand first if it exists
        if (handItem != null)
            sortedItems.Add(handItem);
            
        // Add hammer second if it exists
        if (hammerItem != null)
            sortedItems.Add(hammerItem);
            
        // Add all other items
        sortedItems.AddRange(otherItems);

        // Update the hotbar items list
        hotbarItems = sortedItems;
    }

    private void Update()
    {
        // Check for number keys based on number of items
        for (int i = 0; i < hotbarItems.Count; i++)
        {
            // Map 1 to first item, 2 to second, etc., with 0 being the last item
            KeyCode keyCode;
            if (i == 0)
            {
                keyCode = KeyCode.Alpha1; // First item uses 1
            }
            else if (i == 1)
            {
                keyCode = KeyCode.Alpha2; // Second item uses 2
            }
            else if (i == hotbarItems.Count - 1)
            {
                keyCode = KeyCode.Alpha0; // Last item uses 0
            }
            else
            {
                keyCode = (KeyCode)((int)KeyCode.Alpha3 + (i - 2)); // 3 through 9 for remaining items
            }

            if (Input.GetKeyDown(keyCode))
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
        if (index < 0 || index >= hotbarItems.Count) return;
        
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

        UpdateSlotHighlighting();
    }

    private void UpdateSlotHighlighting()
    {
        for (int i = 0; i < hotbarItems.Count; i++)
        {
            if (hotbarItems[i].slotBackground != null)
            {
                hotbarItems[i].slotBackground.color = i == currentSelectedIndex ? selectedSlotColor : normalSlotColor;
            }
        }
    }

    public bool IsHoldingHammer()
    {
        return currentSelectedIndex >= 0 && 
               currentSelectedIndex < hotbarItems.Count && 
               hotbarItems[currentSelectedIndex].itemName == "Hammer";
    }

    public string GetCurrentItemName()
    {
        if (currentSelectedIndex >= 0 && currentSelectedIndex < hotbarItems.Count)
        {
            return hotbarItems[currentSelectedIndex].itemName;
        }
        return "";
    }
} 