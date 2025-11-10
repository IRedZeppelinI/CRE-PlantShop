terraform {
  required_providers {    
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~>3.0"
    }
    
    random = {
      source  = "hashicorp/random"
      version = "~>3.1"
    }

    postgresql = {
      source  = "cyrilgdn/postgresql"
      version = "~>1.20" 
    }
  }
}


# (Service Principal) 
provider "azurerm" {
  features {}  
  
  subscription_id = ""
  client_id       = ""       
  client_secret   = ""   
  tenant_id       = ""       

  skip_provider_registration = true
}


provider "postgresql" {  
  host     = azurerm_postgresql_flexible_server.postgres.fqdn
  port     = 5432
  username = azurerm_postgresql_flexible_server.postgres.administrator_login
  password = random_password.postgres_password.result  
  
  database = "postgres" 
  
  sslmode  = "require"   
}