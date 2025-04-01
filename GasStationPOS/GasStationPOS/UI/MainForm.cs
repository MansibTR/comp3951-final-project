﻿using GasStationPOS.Core.Data.Models.ProductModels;
using GasStationPOS.Core.Data.Models.TransactionModels;
using GasStationPOS.Core.Services;
using GasStationPOS.Core.Services.Auth;
using GasStationPOS.Core.Services.Inventory;
using GasStationPOS.Core.Services.Transaction_Payment;
using GasStationPOS.UI;
using GasStationPOS.UI.Constants;
using GasStationPOS.UI.MainFormDataSchemas.DataSourceWrappers;
using GasStationPOS.UI.MainFormDataSchemas.DTOs;
using GasStationPOS.UI.UIFormValidation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;

namespace GasStationPOS
{
    /// <summary>
    /// MainForm class for managing the Gas Station Point of Sale (POS) system. 
    /// It handles user interactions with the interface, such as adding products to the cart, 
    /// updating subtotal and remaining balance, and processing transactions. The form includes 
    /// functionality for displaying current date and time, selecting quantities, and managing the cart. 
    /// It also manages fuel transactions, including selecting fuel types and updating fuel price amounts.
    /// 
    /// Author: Mansib Talukder
    /// Author: Jason Lau
    /// Author: Vincent Fung
    /// Date: 19 March 2025
    ///
    public partial class MainForm : Form
    {
        // ======================== CONSTANTS (move to a different file) ========================
        // Fuel prices for different fuel types
        private static readonly decimal fuelRegularPriceCAD = 1.649m;
        private static readonly decimal fuelPlusPriceCAD = 1.849m;
        private static readonly decimal fuelSupremePriceCad = 2.049m;

        // ======================== SERVICES ========================
        private readonly IInventoryService      inventoryService; // for retrieving all retail and fuel product data to display to the UI
        private readonly ITransactionService    transactionService;
        private readonly IAuthenticationService authenticationService;

        // ======================== BINDING SOURCES ========================

        // binding sources allow for UI control data (Text, Label, ListBox contents, etc.) to be AUTOMATICALLY updated when connected to a data source
        // - updates to the data source will be syncronized UI

        // USER CART UNDERLYING DATA (CONTENTS WILL CHANGE)
        private readonly BindingSource userCartProductsBindingSource;

        // SUBTOTAL, AMOUNT TENDERED, AMOUNT REMAINING UNDERLYING DATA SOURCE (WILL CHANGE)
        private readonly BindingSource paymentDataBindingSource;

        // ======================== CONSTANT DATA FOR APPLICATION ========================

        // RETAIL PRODUCT BUTTONS UNDERLYING DATA (CONTENTS DO NOT CHANGE)
        private readonly IEnumerable<RetailProductDTO> retailProductsDataList;

        // UPDATE QUANTITY BUTTONS UNDERLYING DATA (CONTENTS DO NOT CHANGE)
        private readonly IEnumerable<int> retailProductQuantitiesList;

        // ======================== STATE RELATED DATA FOR APPLICATION ========================

        // USER CART DATA SOURCE (CONTENTS WILL CHANGE)
        private readonly BindingList<ProductDTO> userCartProductsDataList; // data changes when user adds/removes items from the cart

        // Payment related Data sources
        // Stores variables to track the subtotal, the amount remaining and the amount tendered by the customer
        private readonly PaymentDataWrapper paymentDataWrapper;

        // Currently selected product quantity
        private int currentSelectedProductQuantity;

        // Flag to manage the activation state of fuel pumps
        private bool fuelPumpsActivated;

        // Variable to store the input for fuel amount and selected fuel price
        private string fuelAmountInput;
        private decimal fuelPrice;

        /// <summary>
        /// Constructor to initialize the MainForm
        /// </summary>
        public MainForm(IInventoryService       inventoryService, 
                        ITransactionService     transactionService,
                        IAuthenticationService  authenticationService) // dependency injection of services
        {
            //setupDatabase(); // Setup the user database ================================================================================================================================================
            InitializeComponent();

            // === Initilize required services ===
            this.inventoryService   = inventoryService;
            this.transactionService = transactionService;
            this.authenticationService = authenticationService;

            // === Initilize Main View UI Underlying Data ===

            currentSelectedProductQuantity = QuantityConstants.DEFAULT_QUANTITY_VALUE; // Initial value of the current selected product quantity
            fuelPumpsActivated = false;
            fuelAmountInput = "";
            fuelPrice = 0.00m;

            // RETAIL PRODUCT BUTTON DATA
            this.retailProductsDataList = inventoryService.GetAllRetailProductData(); // call inventory service which retreives the data from DB)

            // PRODUCT QUANTITY BUTTON DATA
            this.retailProductQuantitiesList = new List<int> { 1, 2, 3, 4, 5, 10, 25, 50, 100, 999 };

            // LIST CART DATA (INITIALLY EMPTY) 
            this.userCartProductsDataList = new BindingList<ProductDTO>();

            // PAYMENT DATA
            this.paymentDataWrapper = new PaymentDataWrapper(); // contains subtotal, amountRemaining, and amountTendered

            // === Binding sources that view ui controls will bind to ===

            this.userCartProductsBindingSource  = new BindingSource(); 
            this.paymentDataBindingSource       = new BindingSource();

            this.userCartProductsBindingSource.DataSource   = this.userCartProductsDataList; // set UI data binding source to the corresponding data stored in this presenter
            this.paymentDataBindingSource.DataSource        = this.paymentDataWrapper;

            // === Update button labels and tag attribute using data sources ===

            this.SetRetailProductButtonDataFromSource(retailProductsDataList);
            this.SetProductQuantityButtonDataFromSource(retailProductQuantitiesList);

            // === Bind UI controls with the Binding sources ===

            this.SetUserCartListBindingSource(userCartProductsBindingSource); // set ui for the user cart (the ListBox)
            this.SetPaymentInfoLabelsBindingSource(paymentDataBindingSource); // set the UI labels for subtotal, amountTendered, and amountRemaining in the main form to the binding source data

            // === ADD EVENT HANDLERS TO EVENTS IN MAIN FORM ===
            AssociateMainFormEvents();

            // === ADD EVENT HANDLERS TO EVENTS IN LOGIN FORM ===
            AssociateLoginFormEvents();
        }

        #region Button Label and Tag Setters

        /// <summary>
        /// Retail product UI Buttons: 
        /// 
        /// The label of each button is set to a value based on the corresponding retail product that it corresponds to
        /// Retail Product data is IEnumerable<RetailProductDTO> (data converted from json file / database in the presenter)
        /// 
        /// Method is called in the presenter - not in this main form class.
        /// </summary>
        public void SetRetailProductButtonDataFromSource(IEnumerable<RetailProductDTO> retailProductsDataList)
        {
            foreach (RetailProductDTO retailProductDTO in retailProductsDataList)
            {
                // get the corresponding button name based on the retail product (ex. productId of 1 -> "btnRp1")
                string retailBtnName = $"{ButtonNamePrefixes.RETAIL_BUTTON_PREFIX}{retailProductDTO.Id}";

                // find the button with the retail button name (ex. find button with name btnRp1
                Button retailProductButton = (Button)this.Controls.Find(retailBtnName, true).First();

                // 1. Set the text of the button to the name attribute in the RetailProductDTO object
                retailProductButton.Text = retailProductDTO.ProductNameDescription;

                // 2. Attach the retailProductDTO object to the button using the Tag attribute (to know which retail product that the clicked button was associated with)
                retailProductButton.Tag = retailProductDTO;

                // ====== EVENT RELATED ======

                // ( More efficient to put here instead of in AssociateAndRaiseViewEvents() )

                // 3. Associate MainFormDataUpdater.AddNewRetailProductToCart function to the click event of each retail product button
                // - Pass in the tag value into the custom event argument for the presenter to be able to access the value
                RetailProductDTO rpDtoReference = ((RetailProductDTO)retailProductButton.Tag);

                retailProductButton.Click += delegate {
                    MainFormDataUpdater.AddNewRetailProductToCart(
                        this.userCartProductsDataList,
                        rpDtoReference,
                        this.paymentDataWrapper,
                        ref currentSelectedProductQuantity
                    );
                };

                // Add UI updating method to event handler (UpdatePayButtonVisibility will execute after the item was added)
                retailProductButton.Click += delegate { UpdatePayButtonVisibility(); }; // Note: if the first event handler throws an exception, it will prevent subsequent handlers from executing, unless the exception is caught and handled.
            }
        }

        /// <summary>
        /// Update quantity UI Buttons:
        /// 
        /// The label of each button is set to a value based on the corresponding quantity that it corresponds to
        /// Quantity data is IEnumerable<int> (data converted from json file / database in the presenter)
        /// 
        /// Method is called in the presenter - not in this main form class.
        /// </summary>
        public void SetProductQuantityButtonDataFromSource(IEnumerable<int> retailProductQuantityDataList)
        {
            foreach (int productQuantityNum in retailProductQuantityDataList)
            {
                string quantityButtonName = $"{ButtonNamePrefixes.QUANTITY_BUTTON_PREFIX}{productQuantityNum}";

                Button updateQuantityButton = (Button)this.Controls.Find(quantityButtonName, true).First();

                // 1. Set the text of the button to the quantity number
                updateQuantityButton.Text = $"{productQuantityNum.ToString()}x";

                // 2. Attack the index of the quantity number value to the button using the Tag attribute (to know which quantity that the clicked button was associated with)
                updateQuantityButton.Tag = productQuantityNum; // save the value to change the quantity to

                // ====== EVENT RELATED ======

                // ( More efficient to put here instead of in AssociateAndRaiseViewEvents() )

                // 3. Associate MainFormDataUpdater.UpdateSelectedProductQuantity function to the click event of each product quantity update button
                // - Pass in the tag value into the custom event argument for the presenter to be able to access the value
                updateQuantityButton.Click += delegate { 
                    MainFormDataUpdater.UpdateSelectedProductQuantity(
                        ref currentSelectedProductQuantity, 
                        (int)updateQuantityButton.Tag
                    );
                };
            }
        }

        #endregion

        #region Binding Source Setters

        /// <summary>
        /// Bind the data source (a collection of ProductDTOs) with the ListCart UI:
        /// 
        /// Gets and sets data from List<ProductDTO> stored in presenter
        /// Bind the product ListBox list data with a list of ProductDTOs (selectedRetailProductsList) stored in the presenter
        /// 
        /// Two way data binding between the listCart and the userCartProductsBindingSource BindingSource data.
        /// 
        /// Method is called in the presenter - not in this main form class.
        /// </summary>
        public void SetUserCartListBindingSource(BindingSource userCartProductsBindingSource)
        {
            listCart.DataSource = userCartProductsBindingSource; // see if this works, otherwise use a DataGridView class
        }

        /// <summary>
        /// Sets the binding sources of the Labels:
        /// labelSubtotal   <-> paymentDataBindingSource - Subtotal (decimal)
        /// labelTendered   <-> paymentDataBindingSource - AmountTendered (decimal)
        /// labelRemaining  <-> paymentDataBindingSource - AmountRemaining (decimal)
        /// </summary>
        /// <param name="paymentDataBindingSource"></param>
        public void SetPaymentInfoLabelsBindingSource(BindingSource paymentDataBindingSource)
        {
            bool formattingEnabled = true; // boolean to determine if the created Binding object will automatically format the payment decimal value
            decimal defaultValueWhenDataSourceNull = PaymentConstants.INITIAL_AMOUNT_DOLLARS; // default value in case the data source is null
            string twoDecimalPlacesString = "C2"; // tells Binding object to format the data source to 2 decimal places (proper currency format)

            // Need DataSourceUpdateMode.OnPropertyChanged so it knows to change the value in the the UI control when the datasource (ex. Subtotal) changes value
            this.labelSubtotal.DataBindings.Add(new Binding("Text", paymentDataBindingSource, "Subtotal", formattingEnabled, DataSourceUpdateMode.OnPropertyChanged, defaultValueWhenDataSourceNull, twoDecimalPlacesString));
            this.labelTendered.DataBindings.Add(new Binding("Text", paymentDataBindingSource, "AmountTendered", formattingEnabled, DataSourceUpdateMode.OnPropertyChanged, defaultValueWhenDataSourceNull, twoDecimalPlacesString));
            this.labelRemaining.DataBindings.Add(new Binding("Text", paymentDataBindingSource, "AmountRemaining", formattingEnabled, DataSourceUpdateMode.OnPropertyChanged, defaultValueWhenDataSourceNull, twoDecimalPlacesString));
        }

        #endregion

        #region Attach Eventhandlers to events of SINGLE Buttons

        /// <summary>
        /// Attaches EventHandler objects to buttons, that do not have data binding properties with data stored in the presenter (retail, fuel and quantity buttons do)
        /// </summary>
        private void AssociateMainFormEvents()
        {
            // === Clear 1 item from cart Button ===

            btnRemoveItem.Click += btnRemoveItem_Click_v2;

            // === Clear ALL products from cart Button ===

            btnClear.Click += delegate { // using delegate operator allows for anonymous methods that return an EventHandler object ex: delegate {<can put any code to handle event in here>}
                MainFormDataUpdater.RemoveAllProductsFromCart(userCartProductsDataList, paymentDataWrapper, ref currentSelectedProductQuantity);
            };
            btnClear.Click += delegate { UpdatePayButtonVisibility(); };


            // === Payment Button ===

            // Call transactionService.CreateTransaction function when either btnPayCard or btnPayCash buttons are clicked (passing in the respective payment method)
            // (Need to assign the corresponding PaymentMethod enum member in paramater so transactionService can can handle the correct payment type
            btnPayCard.Click += delegate { transactionService.CreateTransaction(PaymentMethod.CARD, this.userCartProductsDataList); };
            btnPayCash.Click += delegate { transactionService.CreateTransaction(PaymentMethod.CASH, this.userCartProductsDataList); };
        }

        private void AssociateLoginFormEvents()
        {
            // === Login Button ===
            buttonLogin.Click += buttonLogin_Click;
        }

        #endregion

        // ========= Begin Region of Re-used Updated Event handlers =========

        /// <summary>
        /// Event handler for removing an item from the cart
        /// </summary>
        private void btnRemoveItem_Click_v2(object sender, EventArgs e)
        {
            if (listCart.SelectedIndex != -1 && listCart.SelectedItem != null)
            {
                // Get the selected item from the list                                  
                var selectedProduct = listCart.SelectedItem as ProductDTO;

                if (selectedProduct != null)
                {
                    // Remove the product from the cart
                    MainFormDataUpdater.RemoveProductFromCart(userCartProductsDataList, selectedProduct, paymentDataWrapper); // (remove from the userCartProductsDataList list - will update the listbox UI automatically)

                    // Show pnlProducts, pnlBottomNavMain
                    HidePanels();
                    pnlProducts.Visible = true;
                    pnlBottomNavMain.Visible = true;

                    // if the cart is empty, hide payment buttons
                    UpdatePayButtonVisibility();
                }
            }
        }

        // ========= End Region of Re-used Updated Event handlers =========

        /// <summary>
        /// Event triggered when the form is loaded
        /// </summary>
        private void MainForm_Load(object sender, EventArgs e)
        {
            timerDateTime.Start(); // Start the timer on form load
        }

        /// <summary>
        /// Event triggered to update the date and time display
        /// </summary>
        private void timerDateTime_Tick(object sender, EventArgs e)
        {
            // Update the label with the current date and time
            lblDateTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>
        /// Helper Method to toggle the visibility of pay buttons
        /// </summary>
        private void UpdatePayButtonVisibility()
        {
            if (listCart.Items.Count == 0)
            {
                btnPayCard.Visible = false;
                btnPayCash.Visible = false;
            }
            else
            {
                btnPayCard.Visible = true;
                btnPayCash.Visible = true;
            }
        }

        /// <summary>
        /// Helper method to hide all modified panels
        /// </summary>
        private void HidePanels()
        {
            pnlProducts.Visible = false;
            pnlBottomNavMain.Visible = false;
            pnlBottomNavBack.Visible = false;
            pnlFuelConfirmation.Visible = false;
            pnlFuelTypeSelect.Visible = false;
            pnlSelectCartItem.Visible = false;
            pnlAddFuelAmount.Visible = false;
        }

        /// <summary>
        /// Event handler for selecting a cart item
        /// </summary>
        private void listCart_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listCart.SelectedIndex != -1 && listCart.SelectedItem != null)
            {
                string selectedItem = listCart.SelectedItem.ToString();
                string itemName = selectedItem.Split('\t')[0].Trim();  // Extract item name

                // Update the label in pnlSelectCartItem with the selected item name
                labelSelectedItem.Text = itemName;

                // Show pnlSelectCartItem, pnlBottomNavBack
                HidePanels();
                pnlSelectCartItem.Visible = true;
                pnlBottomNavBack.Visible = true;
            }
        }

        /// <summary>
        /// Helper method to reset the UI to its initial state
        /// </summary>
        private void reset()
        {
            // Show pnlProducts, pnlBottomNavMain
            HidePanels();

            // Unhighlight fuel pumps
            UnhighlightFuelPumps();
            fuelPumpsActivated = false;

            pnlProducts.Visible = true;
            pnlBottomNavMain.Visible = true;

            // Reset the fuel price amount
            labelFuelPrice.Text = "0.00";
            fuelAmountInput = "";
        }

        /// <summary>
        /// Event handler for the back button click
        /// </summary>
        private void btnBack_Click(object sender, EventArgs e)
        {
            reset();
        }

        /// <summary>
        /// Highlights the fuel pump buttons 
        /// by changing their border color to gold and border size to 3.
        /// </summary>
        private void HighlightFuelPumps()
        {
            for (int i = 1; i <= 8; i++)
            {
                Button btn = Controls.Find($"btnFuelPump{i}", true).FirstOrDefault() as Button;
                if (btn != null)
                {
                    btn.FlatAppearance.BorderColor = Color.Gold;
                    btn.FlatAppearance.BorderSize = 3;
                }
            }
        }

        /// <summary>
        /// Resets the appearance of all fuel pump buttons 
        /// by changing their border color to black and border size to 1.
        /// </summary>
        private void UnhighlightFuelPumps()
        {
            for (int i = 1; i <= 8; i++)
            {
                Button btn = Controls.Find($"btnFuelPump{i}", true).FirstOrDefault() as Button;
                if (btn != null)
                {
                    btn.FlatAppearance.BorderColor = Color.Black;
                    btn.FlatAppearance.BorderSize = 1;
                }
            }
        }

        /// <summary>
        /// Handles the "Pay Fuel" button click event, hides other panels, shows the fuel confirmation panel,
        /// and highlights the fuel pumps for selection.
        /// </summary>
        private void btnPayFuel_Click(object sender, EventArgs e)
        {
            // Show pnlFuelConfirmation, pnlBottomNavBack
            HidePanels();
            pnlFuelConfirmation.Visible = true;
            pnlBottomNavBack.Visible = true;

            // Highlight fuel pump buttons (set border color to yellow and border size to 3)
            HighlightFuelPumps();
            fuelPumpsActivated = true;
        }

        /// <summary>
        /// Handles the fuel pump selection by updating the pump number label 
        /// and navigating to the fuel type selection panel. 
        /// Highlights the selected fuel pump and unhighlights the others.
        /// </summary>
        private void btnFuelPump_Click(object sender, EventArgs e)
        {
            if (fuelPumpsActivated == false)
            {
                return;
            }

            // Get the clicked button
            Button btn = sender as Button;

            // Check if the button is valid
            if (btn != null)
            {
                // Update the label with the corresponding pump number
                int pumpNumber = int.Parse(btn.Name.Replace("btnFuelPump", ""));
                labelPumpNum.Text = $"PUMP {pumpNumber}"; // Update label with the correct pump number

                // Show the pnlFuelTypeSelection panel
                HidePanels();
                pnlFuelTypeSelect.Visible = true;
                pnlBottomNavBack.Visible = true;

                UnhighlightFuelPumps();

                // Highlight the selected fuel pump
                btn.FlatAppearance.BorderColor = Color.Gold;
                btn.FlatAppearance.BorderSize = 3;

            }
        }

        /// <summary>
        /// Handles the fuel type selection by updating the label with the selected fuel type 
        /// and showing the fuel amount input panel.
        /// </summary>
        private void btnFuelType_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null)
            {
                // Extract fuel type from the button's text
                string fuelType = btn.Text.ToUpper();

                // Update label with pump number and fuel type
                labelFuelType.Text = $"{labelPumpNum.Text} {fuelType}";

                // Show the add fuel amount panel
                HidePanels();
                pnlAddFuelAmount.Visible = true;
                pnlBottomNavBack.Visible = true;
            }
        }

        /// <summary>
        /// Handles the numeric input for the fuel price calculator by appending or replacing input values.
        /// Updates the fuel price label accordingly.
        /// </summary>
        private void btnFuelCalculator_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null)
            {
                string value = btn.Text;

                // If input is "0", "00", "000", append normally
                if (value == "0" || value == "00" || value == "000")
                {
                    fuelAmountInput += value;
                }

                // If input is a preset amount (e.g., "10.00"), replace entire input
                else if (value.Contains("."))
                {
                    fuelAmountInput = value.Replace(".", ""); // Store without decimal
                }

                // Otherwise, append to the input
                else
                {
                    fuelAmountInput += value;
                }

                UpdateFuelPriceLabel();
            }
        }

        /// <summary>
        /// Updates the fuel price label by converting the input fuel amount to a decimal format and displaying it.
        /// </summary>
        private void UpdateFuelPriceLabel()
        {
            if (string.IsNullOrEmpty(fuelAmountInput))
            {
                labelFuelPrice.Text = "0.00"; // Default if empty
                return;
            }

            // Convert input to decimal format (X.YY)
            decimal amount = decimal.Parse(fuelAmountInput) / 100;
            labelFuelPrice.Text = amount.ToString("0.00");
        }

        /// <summary>
        /// Handles the backspace input for the fuel price calculator, removing the last character from the fuel input.
        /// Updates the fuel price label accordingly.
        /// </summary>
        private void btnFuelCalculatorBackspace_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(fuelAmountInput))
            {
                fuelAmountInput = fuelAmountInput.Substring(0, fuelAmountInput.Length - 1);
                UpdateFuelPriceLabel();
            }
        }

        /// <summary>
        /// Validates the entered fuel amount, calculates the total price, and adds the fuel to the cart if valid.
        /// If invalid, shows an error message.
        /// </summary>
        private void btnFuelCalculatorEnter_Click(object sender, EventArgs e)
        {
            if (decimal.TryParse(labelFuelPrice.Text, out decimal total) && total > 0)
            {
                // Extract pump number and fuel type
                string[] fuelInfo = labelFuelType.Text.Split(' ');
                int pumpNumber = int.Parse(fuelInfo[1]); // Get pump number
                string fuelType = fuelInfo[2]; // Get fuel type

                // Determine the price per liter based on the fuel type
                switch (fuelType.ToUpper())
                {
                    case "REGULAR":
                        fuelPrice = fuelRegularPriceCAD;
                        break;
                    case "PLUS":
                        fuelPrice = fuelPlusPriceCAD;
                        break;
                    case "SUPREME":
                        fuelPrice = fuelSupremePriceCad;
                        break;
                }

                // Calculate quantity up to three decimal places
                decimal quantity = Math.Round(total / fuelPrice, 3);

                // Create a CartItem and add it to the cart
                CartItem newItem = new CartItem(labelFuelType.Text, quantity, fuelPrice, total);
                listCart.Items.Add(newItem);

                //subtotal += total;

                reset();
                //UpdateAfterAddingToCart();
            }
            else
            {
                MessageBox.Show("Please enter a valid fuel amount.");
            }
        }


        // === LOGIN FORM ===

        /// <summary>
        /// Handles all the login logic.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonLogin_Click(object sender, EventArgs e)
        {
            // Retrieve user input
            string enteredUsername  = textBoxAccountID.Text.Trim();
            string enteredPassword  = textBoxPassword.Text;

            // Validate user inputs (check if they are empty)
            bool validationSuccessful = LoginFormValidator.ValidateFields(enteredUsername, enteredPassword);

            if (!validationSuccessful)
            {
                labelLoginError.Text = "Error: Please enter both username and password.";
                labelLoginError.Visible = true;
                return;
            }

            // Validate user credentials
            bool authenticationSuccessful = authenticationService.Authenticate(enteredUsername, enteredPassword);

            if (!authenticationSuccessful)
            {
                labelLoginError.Text = "Error: Username or password incorrect.";
                labelLoginError.Visible = true;
                return;
            }

            // Successful login
            tabelLayoutPanelLogin.Visible = false;  // Hide the login panel
            MessageBox.Show("Login successful!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
