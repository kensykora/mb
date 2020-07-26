terraform {
  required_providers {
    azurerm = "~>2"
  }
  backend "azurerm" {
    resource_group_name  = "shared"
    storage_account_name = "kmstf"
    container_name       = "tfstate"
    key                  = "terraform.tfstate"
  }
}

provider "azurerm" {
  features {}
}
resource "azurerm_resource_group" "mbot_rg" {
  name     = "mbot-rg-${var.env}"
  location = "centralus"
}

resource "azurerm_storage_account" "mbot_storage" {
  name                     = "mbotstg${var.env}"
  resource_group_name      = azurerm_resource_group.mbot_rg.name
  location                 = azurerm_resource_group.mbot_rg.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
}

resource "azurerm_app_service_plan" "mbot_appsvc_plan" {
  name                = "mbot-asp-${var.env}"
  location            = azurerm_resource_group.mbot_rg.location
  resource_group_name = azurerm_resource_group.mbot_rg.name
  kind                = "FunctionApp"

  sku {
    tier = "Dynamic"
    size = "Y1"
  }
}

resource "azurerm_function_app" "mbot_func" {
  name                       = "mbot-func-${var.env}"
  location                   = azurerm_resource_group.mbot_rg.location
  resource_group_name        = azurerm_resource_group.mbot_rg.name
  app_service_plan_id        = azurerm_app_service_plan.mbot_appsvc_plan.id
  storage_account_name       = azurerm_storage_account.mbot_storage.name
  storage_account_access_key = azurerm_storage_account.mbot_storage.primary_access_key

  identity {
    type = "SystemAssigned"
  }
}
