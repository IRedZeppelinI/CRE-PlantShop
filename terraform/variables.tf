#Resource group name
variable "resource_group_name" {
  description = "O nome do Resource Group."
  type        = string
  default     = "cre-pedro-rg" # O nome "fixo" que sugeriste
}

# Location
variable "location" {
  description = "A região onde criar os recursos."
  type        = string
  default     = "WestEurope"
}




# Tags
variable "common_tags" {
  description = "Tags a aplicar a todos os recursos."
  type        = map(string)
  default = {
    "project"   = "CRE-Projeto"
    "createdBy" = "Terraform"
    "author"    = "PedroVerde"    
  }
}

#StorageAccount

# variaveis pra os container names
variable "storage_container_names" {
  description = "Uma lista de nomes de containers a criar na Storage Account."
  type        = list(string)
  default     = ["articles", "posts", "challenges"]
}

variable "storage_account_prefix" {
  description = "Prefixo para a Storage Account."
  type        = string
  default     = "crepedrostorage"
}



# Postgress

variable "postgres_server_prefix" {
  description = "Prefixo para o servidor PostgreSQL Flexible."
  type        = string
  default     = "cre-pg-server" # ex: "cre-pg-server-a1b2c3d4"
}

variable "postgres_admin_login" {
  description = "Nome de utilizador do administrador do Postgres."
  type        = string
  default     = "pgadminuser"
}


variable "postgres_db_name" {
  description = "O nome da base de dados."
  type        = string
  default     = "plantshop-db"
}



# Service Bus

variable "servicebus_namespace_prefix" {
  description = "Prefixo para o Service Bus Namespace."
  type        = string
  default     = "pedroverde-cre-sb" # ex: "cre-sb-ecommerce-a1b2c3"
}

variable "servicebus_queue_name" {
  description = "Nome da queue."
  type        = string
  default     = "orders-logistics"
}


#  Cosmos DB

variable "cosmosdb_account_prefix" {
  description = "Prefixo para a Conta Cosmos DB."
  type        = string
  default     = "pedro-cre-cosmosdb"
}

variable "cosmosdb_database_name" {
  description = "Nome da Base de Dados."
  type        = string
  default     = "PlantShopDb" 
}

variable "cosmosdb_containers" {
  description = "Mapa dos Containers e Partition Keys."
  type        = map(string)
  default = {
    # Nome do Container = Caminho da Partition Key
    "community-posts"      = "/id" 
    "daily-challenges" = "/id" 
  }
}


# Docker Hub (Para o App Service) 

variable "dockerhub_username" {
  description = "Nome de utilizador Docker Hub."
  type        = string  
}

variable "dockerhub_password" {
  description = "Personal Access Token (PAT) do Docker Hub."
  type        = string
  sensitive   = true   
}


#   App Service
variable "app_service_prefix" {
  description = "Prefixo para o nome do App Service."
  type        = string
  default     = "cre-pedro-plantshop"
}