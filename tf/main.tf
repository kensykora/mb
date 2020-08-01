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

data "azurerm_client_config" "current" {
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

resource "azurerm_application_insights" "mbot_ai" {
  name                = "mbot-func-${var.env}-ai"
  location            = azurerm_resource_group.mbot_rg.location
  resource_group_name = azurerm_resource_group.mbot_rg.name
  application_type    = "web"
  retention_in_days   = 30
}

resource "azurerm_key_vault" "mbot_kv" {
  name                = "mbot-kv-${var.env}"
  location            = azurerm_resource_group.mbot_rg.location
  resource_group_name = azurerm_resource_group.mbot_rg.name
  tenant_id           = data.azurerm_client_config.current.tenant_id

  sku_name = "standard"
}

resource "azurerm_function_app" "mbot_func" {
  name                       = "mbot-func-${var.env}"
  location                   = azurerm_resource_group.mbot_rg.location
  resource_group_name        = azurerm_resource_group.mbot_rg.name
  app_service_plan_id        = azurerm_app_service_plan.mbot_appsvc_plan.id
  storage_account_name       = azurerm_storage_account.mbot_storage.name
  storage_account_access_key = azurerm_storage_account.mbot_storage.primary_access_key

  version = "~3"

  identity {
    type = "SystemAssigned"
  }

  app_settings = {
    "telegramApiKey"                 = var.telegramApiKey
    "spotifyClientId"                = var.spotifyClientId
    "spotifyClientSecret"            = var.spotifyClientSecret
    "storageAccountName"             = azurerm_storage_account.mbot_storage.name
    "storageAccountKey"              = azurerm_storage_account.mbot_storage.primary_access_key
    "APPINSIGHTS_INSTRUMENTATIONKEY" = azurerm_application_insights.mbot_ai.instrumentation_key
    "keyVaultName"                   = azurerm_key_vault.mbot_kv.name
  }
}

resource "azurerm_key_vault_access_policy" "mbot_kv_policy_kms" {
  key_vault_id = azurerm_key_vault.mbot_kv.id

  tenant_id = azurerm_function_app.mbot_func.identity[0].tenant_id
  object_id = azurerm_function_app.mbot_func.identity[0].principal_id

  secret_permissions = [
    "get"
  ]
}
