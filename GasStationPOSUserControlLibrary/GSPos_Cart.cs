﻿using System;
using System.Windows.Forms;

namespace GasStationPOSUserControlLibrary
{
    public partial class GSPos_Cart: UserControl
    {
        public GSPos_Cart()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Gets the listCart Items Count
        /// </summary>
        /// <returns></returns>
        public int GetCartItemsCount()
        {
            return listCart.Items.Count;
        }

        /// <summary>
        /// Updates the subtitle and remaining labels
        /// </summary>
        /// <param name="subTotal"></param>
        /// <param name="remaining"></param>
        public void UpdateSubtitleAndRemainingLabels(string subTotal, string remaining)
        {
            // Update the subtotal after removing the item
            labelSubtotal.Text = subTotal;

            // Update Remaining
            labelRemaining.Text = remaining;
        }

        /// <summary>
        /// Adds a CartItem to the Cart
        /// </summary>
        /// <param name="cartItem"></param>
        public void AddItemToCart(CartItem cartItem)
        {
            listCart.Items.Add(cartItem);
        }

        /// <summary>
        /// Clears the cart.
        /// </summary>
        public void ClearCart()
        {
            listCart.Items.Clear();
            labelSubtotal.Text = "";
            labelRemaining.Text = "";
        }

        /// <summary>
        /// Gets the listCart selected Index
        /// </summary>
        /// <returns></returns>
        public Boolean IsListCartEmpty()
        { 
            return listCart.SelectedIndex != -1 && listCart.SelectedItem != null;
        }

        /// <summary>
        /// Returns the selected item as a string
        /// </summary>
        /// <returns></returns>
        public string GetListCartItemString()
        { 
            return listCart.SelectedItem.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public CartItem GetCartItem()
        {
            return (CartItem) listCart.SelectedItem;
        }

        /// <summary>
        /// Removes the selectedItem from the cart.
        /// </summary>
        /// <param name="selectedItem"></param>
        public void RemoveItemFromCart(CartItem selectedItem)
        {
            listCart.Items.Remove(selectedItem);
        }
    }
}
