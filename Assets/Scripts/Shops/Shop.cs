using System;
using System.Collections;
using System.Collections.Generic;
using GameDevTV.Inventories;
using GameDevTV.Saving;
using RPG.Control;
using RPG.Inventories;
using RPG.Stats;
using UnityEngine;

namespace RPG.Shops
{
    public class Shop : MonoBehaviour, IRaycastable, ISaveable
    {
        [SerializeField] private string shopName;
        [Range(0,100)]
        [SerializeField] private float sellingPercentage = 80f;
        [SerializeField] private StockItemConfig[] stockConfig;
        [SerializeField] private float maximumBarterDiscont = 80f;
        
        [System.Serializable]
        private class StockItemConfig
        {
            public InventoryItem item;
            public int initialStock;
            
            [Range(0,100)]
            public float buyingDiscountPercentage;

            public int levelToUnlock = 0;
        }

        private Dictionary<InventoryItem, int> transaction = new Dictionary<InventoryItem, int>();
        private Dictionary<InventoryItem, int> stockSold = new Dictionary<InventoryItem, int>();

        private Shopper currentShopper = null;

        private bool isBuyingMode = true;

        private ItemCategory filter = ItemCategory.None;

        public event Action onChange;

      
        public IEnumerable<ShopItem> GetFilteredItems()
        {
            foreach (ShopItem item in GetAllItems())
            {
                if (filter == ItemCategory.None || item.GetInventoryItem().GetCategory() == filter)
                {
                    yield return item;
                }
            }

        }
        
        public IEnumerable<ShopItem> GetAllItems()
        {

            Dictionary<InventoryItem, float> prices = GetPrices();
            Dictionary<InventoryItem, int> availabilities = GetAvailabilities();
            foreach (InventoryItem item in availabilities.Keys)
            {
                if(availabilities[item] <= 0) continue;

                float price = prices[item];
                int quantityInTransaction = 0;
                transaction.TryGetValue(item, out quantityInTransaction);
                int availability = availabilities[item];
                yield return new ShopItem(item, availability, price,quantityInTransaction);
            }
        }

        private Dictionary<InventoryItem, int> GetAvailabilities()
        {
            Dictionary<InventoryItem, int> availabilities = new Dictionary<InventoryItem, int>();
            
            foreach (var config in GetAvailableConfigs())
            {
                if (isBuyingMode)
                {
                    if (!availabilities.ContainsKey(config.item))
                    {
                        int sold = 0;
                        stockSold.TryGetValue(config.item, out sold);
                        availabilities[config.item] = -sold;
                    }

                    availabilities[config.item] += config.initialStock;
                }
                else
                {
                    availabilities[config.item] = CountItemsInInventory(config.item);
                }


            }
            return availabilities;
        }

        private Dictionary<InventoryItem, float> GetPrices()
        {
            Dictionary<InventoryItem, float> prices = new Dictionary<InventoryItem, float>();
            foreach (var config in GetAvailableConfigs())
            {
                if (isBuyingMode)
                {
                    if (!prices.ContainsKey(config.item))
                    {
                        prices[config.item] = config.item.GetPrice() * GetBarterDiscount();
                    }

                    prices[config.item] *= (1- config.buyingDiscountPercentage/100);
                }
                else
                {
                    prices[config.item] = config.item.GetPrice() * (sellingPercentage / 100);
                }
            }

            return prices;
        }

        private float GetBarterDiscount()
        {
            BaseStats baseStats = currentShopper.GetComponent<BaseStats>();
            float discount = baseStats.GetStat(Stat.BuyingDiscountPecentage);
            return (1 - Mathf.Min(maximumBarterDiscont,discount) / 100);
        }

        private IEnumerable<StockItemConfig> GetAvailableConfigs()
        {
            int shopperLevel = GetShopperLevel();
            foreach (var config in stockConfig)
            {
                if(config.levelToUnlock > shopperLevel) continue;
                yield return config;
            }
        }
        
        private int CountItemsInInventory(InventoryItem item)
        {
            int total = 0;
            Inventory inventory = currentShopper.GetComponent<Inventory>();
            if (inventory == null)
            {
                return 0;
            }

            for (int i = 0; i < inventory.GetSize(); i++)
            {
                if (inventory.GetItemInSlot(i) == item)
                {
                    total += inventory.GetNumberInSlot(i);
                }
            }

            return total;
        }
        
        public void SetShopper(Shopper shopper)
        {
            currentShopper = shopper;
        }

        public void ConfirmTransaction()
        {
            Inventory shopperInventory = currentShopper.GetComponent<Inventory>();
            Purse shopperPurse = currentShopper.GetComponent<Purse>();

            if (shopperInventory == null || shopperPurse == null)
            {
                return;
            }

            foreach (ShopItem shopItem in GetAllItems())
            {

                InventoryItem item = shopItem.GetInventoryItem();
                int quantity = shopItem.GetQuantityInTransaction();
                float price = shopItem.GetPrice();
                for (int i = 0; i < quantity; i++)
                {

                    if (isBuyingMode)
                    {
                        BuyItem(shopperPurse, price, shopperInventory, item);
                    }
                    else
                    {
                        SellItem(shopperPurse, price, shopperInventory, item);
                    }
                }
            }

            if (onChange != null)
            {
                onChange();
            }

        }

        private void SellItem(Purse shopperPurse, float price, Inventory shopperInventory, InventoryItem item)
        {
            int slot = FindFirstItemSlot(shopperInventory, item);
            if (slot == -1)
            {
                return;
            }
            AddToTransaction(item, -1);
            shopperInventory.RemoveFromSlot(slot, 1);
            if (!stockSold.ContainsKey(item))
            {
                stockSold[item] = 0;
            }

            stockSold[item]--;
            shopperPurse.UpdateBalance(price);
        }

        private int FindFirstItemSlot(Inventory shopperInventory, InventoryItem item)
        {
            for (int i = 0; i < shopperInventory.GetSize(); i++)
            {
                if (shopperInventory.GetItemInSlot(i) == item)
                {
                    return i;
                }
            }

            return -1;
        }

        private void BuyItem(Purse shopperPurse, float price, Inventory shopperInventory, InventoryItem item)
        {
            if (shopperPurse.GetBalance() < price)
            {
                return;
            }

            bool success = shopperInventory.AddToFirstEmptySlot(item, 1);
            if (success)
            {
                AddToTransaction(item, -1);
                if (!stockSold.ContainsKey(item))
                {
                    stockSold[item] = 0;
                }
                stockSold[item]++;
                shopperPurse.UpdateBalance(-price);
            }
        }

        public void SelectFilter(ItemCategory category)
        {
            print(category);
            filter = category;
            if (onChange != null)
            {
                onChange();
            }
        }

        public ItemCategory GetFilter()
        {
            return filter;
        }

        public void SelectMode(bool isBuying)
        {
            isBuyingMode = isBuying;
            if (onChange != null)
            {
                onChange();
            }
        }

        public bool IsBuyingMode()
        {
            return isBuyingMode;
        }

        public bool CanTransact()
        {
            if (IsTransactionEmpty())
            {
                return false;
            }

            if (!HasSufficientFunds())
            {
                return false;
            }

            if (!HasInventorySpace())
            {
                return false;
            }

            return true;
        }

        public bool HasInventorySpace()
        {
            if (!isBuyingMode)
            {
                return true;
            }
            
            List<InventoryItem> flatItems = new List<InventoryItem>();
            foreach (ShopItem shopItem in GetAllItems())
            {
                InventoryItem item = shopItem.GetInventoryItem();
                int quantity = shopItem.GetQuantityInTransaction();
                for (int i = 0; i < quantity; i++)
                {
                    flatItems.Add(item);
                }
            }

            Inventory shopperInventory = currentShopper.GetComponent<Inventory>();
            if (shopperInventory == null)
            {
                return false;
            }

            return shopperInventory.HasSpaceFor(flatItems);

        }

        public bool HasSufficientFunds()
        {
            if (!isBuyingMode)
            {
                return true;
            }

            Purse purse = currentShopper.GetComponent<Purse>();
            if (purse == null) return false;
            return purse.GetBalance() >= TransactionTotal();

        }

        private bool IsTransactionEmpty()
        {
            return transaction.Count == 0;
        }

        public float TransactionTotal()
        {
            
            
            float total = 0;
            foreach (ShopItem item in GetAllItems())
            {
                total += item.GetPrice() * item.GetQuantityInTransaction();
            }

            return total;
        }

        public void AddToTransaction(InventoryItem item, int quantity)
        {
            
           if (!transaction.ContainsKey(item))
           {
               transaction[item] = 0;
           }

           var availabilities = GetAvailabilities();
           int availability = availabilities[item];
           if (transaction[item] + quantity > availability)
           {
               transaction[item] = availability;
           }
           else
           {
               transaction[item] += quantity;
           }
           
           if (transaction[item] <= 0)
           {
               transaction.Remove(item);
           }

           if (onChange != null)
           {
               onChange();
           }
        }

        public bool HandleRaycast(PlayerController callingController)
        {
            if (Input.GetMouseButtonDown(0))
            {
                callingController.GetComponent<Shopper>().SetActiveShop(this);
            }

            return true;
        }

        public CursorType GetCursorType()
        {
            return CursorType.Shop;
        }

        public string GetShopName()
        {
            return shopName;
        }

        private int GetShopperLevel()
        {
            BaseStats stats = currentShopper.GetComponent<BaseStats>();
            if (stats == null)
            {
                return 0;
            }

            return stats.GetLevel();
        }

        public object CaptureState()
        {
            Dictionary<string, int> saveObject = new Dictionary<string, int>();
            foreach (var pair in stockSold)
            {
                saveObject[pair.Key.GetItemID()] = pair.Value;
            }

            return saveObject;
        }

        public void RestoreState(object state)
        {
            Dictionary<string, int> saveObject = (Dictionary<string, int>) state;
            stockSold.Clear();
            foreach (var pair in saveObject)
            {
                stockSold[InventoryItem.GetFromID(pair.Key)] = pair.Value;
            }
        }
    }

}
