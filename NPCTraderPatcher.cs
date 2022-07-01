﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace GhostloreAP
{
    public class ItemFactory : IGLAPSingleton
    {
        public static ItemFactory instance
        {
            get
            {
                if (_inst == null)
                {
                    _inst = new ItemFactory();
                }
                return _inst;
            }
        }

        private static ItemFactory _inst;

        private Item referenceItem;

        private Sprite braceletSprite;

        public Sprite Bracelet { get { return braceletSprite; } }

        public List<Item> archipelagoShopItems { get; private set; }

        public string[] shopItemNames;

        public void Init()
        {
            referenceItem = ItemManager.instance.GetItemFromName("Sona");
            archipelagoShopItems = new List<Item>();
            shopItemNames = new string[20];
            braceletSprite = SpriteFactory.LoadSprite("bracelet.png");

        }

        public void CacheShopItemNames()
        {
            GLAPClient.instance.GetShopEntryNamesAsync((i,name) =>
            {
                shopItemNames[i] = name;
            });
        }

        public void Cleanup()
        {
            referenceItem = null;
            _inst = null;
        }


        public void AddItemToInventory(int slot,string name, string description, int cost, Item item,CharacterContainer character)
        {
            ItemInstance itemInstance = Singleton<ItemManager>.instance.SpawnItem(item, 1, 1, 0f, character.transform.position, character.transform.position);

            Item newItem = new Item();

            Traverse.Create(newItem).Field("itemName").SetValue(name);
            Traverse.Create(newItem).Field("description").SetValue(description);
            Traverse.Create(newItem).Field("icon").SetValue(braceletSprite);
            Traverse.Create(newItem).Field("cost").SetValue(cost);
            Traverse.Create(newItem).Field("maxStackSize").SetValue(1);
            Traverse.Create(newItem).Field("attributes").SetValue(ItemAttributes.None);
            Traverse.Create(newItem).Field("soundFX").SetValue(null);
            Traverse.Create(newItem).Field("levelMin").SetValue(1);
            Traverse.Create(newItem).Field("levelMax").SetValue(1);
            Traverse.Create(newItem).Field("rarityChances").SetValue(new float[1] { 0 });
            Traverse.Create(newItem).Field("tags").SetValue(referenceItem.Tags);
            Traverse.Create(newItem).Field("coreModifiers").SetValue(referenceItem.CoreModifiers);
            Traverse.Create(newItem).Field("instanceComponents").SetValue(referenceItem.InstanceComponents);
            Traverse.Create(newItem).Field("weaponSprite").SetValue(referenceItem.WeaponSprite);
            Traverse.Create(newItem).Field("tattoo").SetValue(referenceItem.Tattoo);

            GLAPNotification.instance.DisplayMessage("AP_ShopSlot=" + slot);

            ExtendedBindingManager.instance.RegisterAndSet<XItemInstance>(itemInstance, (s) =>
            {
                s.overrideItem = newItem;
                s.cost = cost;
                s.AP_ShopSlot = slot;
            });
            itemInstance.PickupOffGround(character);
        }


        public void SetupArchipelagoShop(NPCTrader trader)
        {
            var traderCharacter = trader.ParentCharacter;
            for (int i = 0; i < 20; i++)
            {
                if (GLAPClient.instance.ShopAlreadyChecked(i)) { continue; }
                AddItemToInventory(i, shopItemNames[i], "A strange bracelet that seems to link items between worlds...", GLAPSettings.baseItemShopCost, referenceItem, traderCharacter);
                
            }

        }

        
    }

    [HarmonyPatch(typeof(NPCTrader),"CheckInventory")]
    public class NPCTraderPatcher
    {
        static void Postfix(NPCTrader __instance,ref int ___lastRestockDay, ref Inventory ___inventory)
        {
            for (int i = ___inventory.Items.Count - 1; i >= 0; i--)
            {
                ___inventory.Items[i].DestroyItem();
            }

            ___lastRestockDay = ___lastRestockDay - 1;
            if (___lastRestockDay < 0)
            {
                ___lastRestockDay = 365;
            }

            ItemFactory.instance.SetupArchipelagoShop(__instance);

        }
    }
}
