output "resource_group_name" {
  description = "O nome do Resource Group criado."
  value       = azurerm_resource_group.rg.name
}

output "storage_account_name" {
  description = "O nome da Storage Account."
  value       = azurerm_storage_account.storage.name
}

output "storage_account_connection_string" {
  description = "A Connection String principal para o Storage Account."
  value       = azurerm_storage_account.storage.primary_connection_string
  sensitive   = true 
}

output "storage_account_primary_access_key" {
  description = "A Chave de Acesso primária"
  value       = azurerm_storage_account.storage.primary_access_key
  sensitive   = true
}


# Postgress
output "postgres_server_name" {
  description = "O nome do servidor PostgreSQL."
  value       = azurerm_postgresql_flexible_server.postgres.name
}

output "postgres_host_name" {
  description = "O FQDN (host) do servidor Postgres."
  value       = azurerm_postgresql_flexible_server.postgres.fqdn
}

output "postgres_admin_login" {
  description = "O nome de utilizador admin do Postgres."
  value       = var.postgres_admin_login
}

output "postgres_admin_password" {
  description = "A palavra-passe do admin."
  value       = random_password.postgres_password.result
  sensitive   = true
}


output "postgres_db_name" {
  description = "O nome da base de dados criada."
  value       = postgresql_database.app_db.name
}


output "postgres_connection_string_uri" {
  description = "Connection string (Formato URI)."
  
  value = "postgres://${var.postgres_admin_login}:${random_password.postgres_password.result}@${azurerm_postgresql_flexible_server.postgres.fqdn}/${postgresql_database.app_db.name}?sslmode=require"
  sensitive = true
}

output "postgres_connection_string_keyvalue" {
  description = "Connection string (Formato Key-Value)." #Usar este, não o uri para a connection string
  
  value = "Host=${azurerm_postgresql_flexible_server.postgres.fqdn};Port=5432;Database=${postgresql_database.app_db.name};Username=${var.postgres_admin_login};Password=${random_password.postgres_password.result};SslMode=Require"
  sensitive = true
}



# --- Service Bus ---

output "servicebus_namespace_name" {
  description = "O nome do Service Bus Namespace."
  value       = azurerm_servicebus_namespace.sb_namespace.name
}

output "servicebus_queue_name" {
  description = "O nome da queue."
  value       = azurerm_servicebus_queue.sb_queue.name
}

output "servicebus_connection_string" {
  description = "A Connection String principal do Service Bus."
  value       = azurerm_servicebus_namespace.sb_namespace.default_primary_connection_string
  sensitive   = true
}


# --- Outputs do Cosmos DB ---

output "cosmosdb_account_name" {
  description = "O nome da Conta Cosmos DB."
  value       = azurerm_cosmosdb_account.cosmos.name
}

output "cosmosdb_endpoint" {
  description = "O Endpoint (URL) da Conta Cosmos DB."
  value       = azurerm_cosmosdb_account.cosmos.endpoint
}

output "cosmosdb_primary_key" {
  description = "A Chave Primária de acesso ao Cosmos DB."
  value       = azurerm_cosmosdb_account.cosmos.primary_key
  sensitive   = true
}

output "cosmosdb_database_name" {
  description = "O nome da Base de Dados lógica."
  value       = azurerm_cosmosdb_sql_database.cosmos_db.name
}

output "cosmosdb_connection_string" {
  description = "A Connection String (Key/Value) para o Cosmos DB."
  value       = "AccountEndpoint=${azurerm_cosmosdb_account.cosmos.endpoint};AccountKey=${azurerm_cosmosdb_account.cosmos.primary_key};"
  sensitive   = true
}


# App Service

output "app_service_default_url" {
  description = "O URL do App Service."
  value       = azurerm_linux_web_app.app.default_hostname
}

output "app_service_name" {
  description = "O nome da App Service."
  value       = azurerm_linux_web_app.app.name
}