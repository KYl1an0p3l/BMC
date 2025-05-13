using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

[Tool]
[GlobalClass]
public partial class Inventory : Resource
{
	[Export]
	public Godot.Collections.Array<InventorySlot> Slots { get; set; } = new();
	public event Action OnUpdated;
	public void Insert(InventoryItems item){
        for (int i = 0; i < Slots.Count; i++){
            var slot = Slots[i];
            if (slot != null && slot.Item == item){ //Si l'item doit être stacké
                slot.amount += 1;
                OnUpdated?.Invoke(); // émet l'événement
                return;
            }
        }
		for(int i=0; i<Slots.Count;i++){
			if(Slots[i] == null){
				Slots[i] = new InventorySlot();
				Slots[i].Item = item;
                Slots[i].amount = 1;
                OnUpdated?.Invoke(); // émet l'événement
				return;
			}
		}
		
	}
    public void removeItemAtIndex(int index){
        Slots[index] = new InventorySlot();
    }
    public void insertSlot(int index, InventorySlot inventorySlot){
        int oldIndex = -1;
        for (int i = 0; i < Slots.Count; i++) {
            if (Slots[i] == inventorySlot) {
                oldIndex = i;
                break;
            }
        }
        if(oldIndex != -1){
            removeItemAtIndex(oldIndex);
        }
        Slots[index] = inventorySlot;
    }
}
