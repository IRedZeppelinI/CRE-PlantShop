#  Resource Group
resource "azurerm_resource_group" "rg" {
  name     = var.resource_group_name
  location = var.location
  tags     = var.common_tags
}

# Storage Account
resource "random_string" "storage_suffix" {
  length  = 8
  special = false 
  upper   = false 
  numeric = true  
}


resource "azurerm_storage_account" "storage" {
  
  name                = "${var.storage_account_prefix}${random_string.storage_suffix.result}"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location 
  
  account_tier             = "Standard"
  account_replication_type = "LRS"
  access_tier              = "Hot"
  
  min_tls_version               = "TLS1_2"
  public_network_access_enabled = true
  is_hns_enabled                = false   

  tags = var.common_tags
}


resource "azurerm_storage_container" "app_containers" {
  
  for_each = toset(var.storage_container_names)

  name                  = each.key 
  storage_account_name  = azurerm_storage_account.storage.name  
  
  container_access_type = "blob" 
}



# PostgreSQL Flexible Server 
resource "random_string" "postgres_suffix" {
  length  = 8
  special = false
  upper   = false
  numeric = true
}

resource "random_password" "postgres_password" {
  length           = 16
  special          = true
  
  override_special = "!#$%&" 
}

resource "azurerm_postgresql_flexible_server" "postgres" {
  name                = "${var.postgres_server_prefix}-${random_string.postgres_suffix.result}"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location

  
  sku_name = "B_Standard_B1ms" 
  
  administrator_login    = var.postgres_admin_login
  administrator_password = random_password.postgres_password.result
  version                = "16" 

 
  storage_mb = 32768 # 32 GiB 

  
  backup_retention_days = 7

  
  public_network_access_enabled = true 

  tags = var.common_tags

  lifecycle {
    ignore_changes = [
      zone,
    ]
  }
}

#  REGRA DE FIREWALL
resource "azurerm_postgresql_flexible_server_firewall_rule" "allow_all" {
  name                = "allow-all-ips-for-dev"
  server_id           = azurerm_postgresql_flexible_server.postgres.id
  start_ip_address    = "0.0.0.0"
  end_ip_address      = "255.255.255.255"
}

resource "postgresql_database" "app_db" {
  name  = var.postgres_db_name
  owner = azurerm_postgresql_flexible_server.postgres.administrator_login

  depends_on = [
    azurerm_postgresql_flexible_server_firewall_rule.allow_all
  ]
}



#  Service Bus
resource "random_string" "servicebus_suffix" {
  length  = 6
  special = false
  upper   = false
  numeric = true
}


resource "azurerm_servicebus_namespace" "sb_namespace" {
  name                = "${var.servicebus_namespace_prefix}-${random_string.servicebus_suffix.result}"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location

  
  sku = "Basic" 

  tags = var.common_tags
}

resource "azurerm_servicebus_queue" "sb_queue" {
  name = var.servicebus_queue_name
  
  namespace_id = azurerm_servicebus_namespace.sb_namespace.id

  dead_lettering_on_message_expiration = true 
  max_delivery_count = 10 
}


#   Cosmos DB Serverless
resource "random_string" "cosmos_suffix" {
  length  = 6
  special = false
  upper   = false
  numeric = true
}


resource "azurerm_cosmosdb_account" "cosmos" {
  name                = "${var.cosmosdb_account_prefix}-${random_string.cosmos_suffix.result}"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  offer_type          = "Standard" 
  kind                = "GlobalDocumentDB" 
  
  capabilities {
    name = "EnableServerless"
  }
  
  consistency_policy {
    consistency_level = "Session"
  }
  
  geo_location {
    location          = azurerm_resource_group.rg.location
    failover_priority = 0
  }

  tags = var.common_tags
}

resource "azurerm_cosmosdb_sql_database" "cosmos_db" {
  name                = var.cosmosdb_database_name
  resource_group_name = azurerm_resource_group.rg.name
  account_name        = azurerm_cosmosdb_account.cosmos.name
  
  # Ao omitir throughput é usado o modo Serverless 
}

resource "azurerm_cosmosdb_sql_container" "cosmos_containers" {
  
  for_each = var.cosmosdb_containers

  name                = each.key 
  resource_group_name = azurerm_resource_group.rg.name
  account_name        = azurerm_cosmosdb_account.cosmos.name
  database_name       = azurerm_cosmosdb_sql_database.cosmos_db.name
  
  # "/id"
  partition_key_paths = [each.value]  
  
  partition_key_version = 2
  
}




#  App Service Plan

resource "azurerm_service_plan" "plan" {
  name                = "cre-app-plan"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location  
  
  os_type             = "Linux" 
  
  sku_name            = "B1" # F1 por vezes dá erro de não existir disponivel na região, ou começa com QuotaExceed.
}


# App Service
resource "random_string" "app_suffix" {
  length  = 6
  special = false
  upper   = false
  numeric = true
}

resource "azurerm_linux_web_app" "app" {
  name                = "${var.app_service_prefix}-${random_string.app_suffix.result}"
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location
  
  
  service_plan_id     = azurerm_service_plan.plan.id
  
  
  app_settings = {
    "WEBSITES_PORT" = "8080"
        
    
    # ConnectionStrings
    "ConnectionStrings__DefaultConnection" = "Host=${azurerm_postgresql_flexible_server.postgres.fqdn};Port=5432;Database=${postgresql_database.app_db.name};Username=${var.postgres_admin_login};Password=${random_password.postgres_password.result};SslMode=Require"
    "ConnectionStrings__StorageAccount"    = azurerm_storage_account.storage.primary_connection_string
    "ConnectionStrings__ServiceBus"        = azurerm_servicebus_namespace.sb_namespace.default_primary_connection_string
    "ConnectionStrings__CosmosDb"          = "AccountEndpoint=${azurerm_cosmosdb_account.cosmos.endpoint};AccountKey=${azurerm_cosmosdb_account.cosmos.primary_key};"
    
    # AzureServiceBus
    "AzureServiceBus__QueueName" = azurerm_servicebus_queue.sb_queue.name
    
    # CosmosDbSettings
    "CosmosDbSettings__DatabaseName"              = azurerm_cosmosdb_sql_database.cosmos_db.name
    "CosmosDbSettings__CommunityPostsContainer"   = "community-posts" 
    "CosmosDbSettings__DailyChallengesContainer"  = "daily-challenges" 
  }

  #  DOCKER HUB 
  site_config {

    always_on = false

    # Define que vamos usar o Docker Hub (Privado)
    application_stack {
      docker_image_name = "${var.dockerhub_username}/cre-plantshop:latest"

      docker_registry_url           = "https://index.docker.io"
      docker_registry_username      = var.dockerhub_username
      docker_registry_password      = var.dockerhub_password
    }   
    
    
    
    app_command_line              = "" # Dockerfile já tem  ENTRYPOINT
  }

  tags = var.common_tags
}