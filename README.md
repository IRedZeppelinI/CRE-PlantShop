# PlantShop (Projeto CRE CLOUD-89104/2025)

Este é o repositório do projeto final CRE **CLOUD-89104/2025**. O objetivo foi desenvolver uma solução de software *cloud-native* para entusiastas de botânica, utilizando uma arquitetura moderna e serviços PaaS (Platform-as-a-Service) do Microsoft Azure.

A aplicação está totalmente "containerizada" e é implementada através de um pipeline de CI/CD automatizado com GitHub Actions.

## Funcionalidades Principais

A plataforma PlantShop divide-se em três grandes áreas:

1.  **Loja (E-Commerce):**

      * Visualização de produtos (plantas, flores, acessórios) por categorias.
      * Detalhes de produto.
      * Carrinho de compras e processo de *checkout*.
      * Área de cliente para visualização do histórico de encomendas.

2.  **Comunidade (Social):**

      * **Desafio Diário:** Diariamente, uma foto de uma planta é publicada e os utilizadores podem tentar adivinhar a espécie correta.
      * **Fórum de Ajuda:** Utilizadores podem fazer *upload* de uma foto de uma planta e pedir ajuda à comunidade para a identificar.

3.  **Backoffice (Administração):**

      * Interface de gestão (CRUD) para **Artigos** e **Categorias** da loja.
      * Gestão de **Encomendas**, com funcionalidade para marcar uma encomenda como "Enviada" (o que dispara uma mensagem para a "logística").
      * Gestão de **Utilizadores** (atribuir/remover *roles*).
      * Gestão do **Desafio Diário** (publicar um novo desafio).

## Arquitetura e Justificação Tecnológica

A aplicação segue os princípios da **Clean Architecture**, separando as responsabilidades em quatro projetos principais: `Domain`, `Application`, `Infrastructure` e `WebUI`.

### Stack Tecnológica

  * **Backend:** .NET 9 (ASP.NET Core Web App)
  * **Autenticação:** ASP.NET Core Identity
  * **Arquitetura:** Clean Architecture
  * **Bases de Dados:** Azure PostgreSQL (Relacional) e Azure Cosmos DB (NoSQL)
  * **Serviços Cloud:** Azure App Service, Azure Blob Storage, Azure Service Bus
  * **DevOps:** GitHub Actions (CI/CD), Docker & Docker Hub (Private Repo), Terraform (IaC)

### Justificação dos Serviços Azure

A escolha dos serviços azure utilizados no projeto:

  * **Azure PostgreSQL (Dados Relacionais):**

      * **Uso:** Armazena os dados da Loja (`Articles`, `Categories`, `Orders`, `Users`) e as tabelas do ASP.NET Core Identity (`AspNetUsers`, `AspNetRoles`, etc.).
      * **Porquê:** Estes dados são **estruturados e relacionais**. Uma encomenda (`Order`) *tem* de estar ligada a um utilizador (`AppUser`) e *contém* múltiplos itens (`OrderItems`) que se ligam a artigos (`Article`). A natureza transacional (ACID) de um motor SQL é essencial para garantir a integridade de uma venda ou a gestão de *stock*.

  * **Azure Cosmos DB (Dados NoSQL):**

      * **Uso:** Armazena os dados da Comunidade (`DailyChallenge`, `CommunityPost` e os seus comentários/respostas).
      * **Porquê:** Estes dados são **documentais e dinâmicos**. Um `CommunityPost` contém uma coleção de `Comments`, e um `DailyChallenge` contém uma coleção de `Guesses`. Este modelo de "documento aninhado" é perfeito para o Cosmos DB, permitindo-nos guardar e ler um *post* inteiro (com todos os seus comentários) numa única operação de leitura, o que é extremamente rápido e escalável para conteúdo gerado pelo utilizador.

  * **Azure Blob Storage:**

      * **Uso:** Armazena todos os ficheiros binários (imagens).
      * **Porquê:** A solução mais económica e simples para armazenar *assets* estáticos (fotos de artigos, *uploads* de utilizadores, imagens dos desafios), servindo-os diretamente para o cliente.

  * **Azure Service Bus:**

      * **Uso:** Message Queue (`orders-logistics`).
      * **Porquê:** Garante o **desacoplamento** de serviços. Quando um Admin marca uma encomenda como "Enviada", a aplicação `WebUI` não precisa de saber quem é a "Logística". Ela apenas coloca uma mensagem na queue, pronta a ser consumida por outra entidade/serviço.

  * **Azure App Service (para Containers) & Docker Hub:**

      * **Uso:** *Host* da aplicação web.
      * **Porquê:** O App Service é um serviço PaaS totalmente gerido que pode ser usado com *containers* (como a pipeline actual exige). É usado o Docker Hub (Privado) como alternativa económica ao Azure Container Registry (ACR) de forma a servir o App Service com a imagem que criamos na pipeline.

-----

## Guia de Instalação e Deployment

O *deployment* da infraestrutura é gerido localmente com Terraform, e o *deployment* da aplicação é gerido pela *pipeline* de CI/CD.

### Pré-requisitos para a máquina local (devido ao uso de agente local -> runs-on: self-hosted)

Para executar a pipeline o agente local necessita de:

  * Terraform CLI
  * Azure CLI
  * NET 9 SDK
  * Docker Desktop

### Passo 1: Executar o Terraform (Localmente)

A infraestrutura é criada *uma vez* através do Terraform. Tendo em conta a necessidade credenciais de um Service Principal do Azure, para evitar commits acidentais, recomendo o seguinte:

1.  Copiar a pasta /terraform deste projeto para um local seguro fora do repositório Git.
2.  Navegar para essa nova pasta copiada.
3.  Editar o ficheiro providers.tf e preencher com as credenciais do respectivo Service Principal.
4.  Executar terraform init para inicializar os providers.
5.  Executar terraform apply e confirmar com yes. (Irá solicitar o token de Docker hub e username fornecidos à parte por email)

O Terraform irá criar todos os serviços Azure. Além disso, o terraform irá configurar o App Service com todas as **Env Variables** necessárias (como as *connection strings* do CosmosDB, ServiceBus, etc.) automaticamente.

### Passo 2: Configurar os GitHub Secrets

A *pipeline* de CI/CD precisa de secrets para aceder aos serviços. É necessário configurar os seguintes *secrets* no repositório do GitHub (em **Settings -> Secrets and variables -> Actions**):

**Credenciais do Azure (Service Principal):**

  * `AZURE_CLIENT_ID`
  * `AZURE_CLIENT_SECRET`
  * `AZURE_TENANT_ID`
  * `AZURE_SUBSCRIPTION_ID`

**Credenciais do Docker Hub:**

  * `DOCKERHUB_USERNAME`
  * `DOCKERHUB_TOKEN` (Um *Personal Access Token* gerado no Docker Hub e fornecido neste caso por email na entrega do projecto).

**Outputs do Terraform:**
Estes são os valores que a *pipeline* precisa de saber para correr as migrações e o *deploy*. É necessário obtê-los da *output* do `terraform apply` (ou com `terraform output [nome]` para as connection strings uma vez que estão marcadas como *sensitive* no terraform) e adicioná-los como *secrets* no GiHub.

  * `AZURE_POSTGRES_CONNECTION`: (Necessário para o `dotnet ef database update`). Utilizar **postgres_connection_string_keyvalue** em vez de postgres_connection_string_uri!
  * `AZURE_STORAGE_CONNECTION`: (Necessário para o *seed* das imagens).
  * `AZURE_WEBAPP_NAME`: (O nome do App Service para onde fazer o *deploy*).

### Passo 3: Correr a Pipeline (CI/CD)

A *pipeline* (`.github/workflows/main.yml`) está configurada para correr automaticamente num `push` à *branch* `main` ou manualmente.

Ao correr a *pipeline* (especialmente na primeira vez), é possível controlar o *seed* das imagens:

  * **`RUN_IMAGE_SEED`:** (Default: `true`). Uma variável no topo do *job* `deploy-to-azure` que pode ser alterada para `false` se não for necessário (re)fazer o *upload* das imagens dos artigos para o Blob Storage. Como o upload está configurado a usar *overwrite*, pode permancecer true se preferir.

-----

## Detalhes do Pipeline (CI/CD)

O *pipeline* está dividido em dois *jobs* principais:

### 1\. Job: `build-and-test` (Continuous Integration)

Este *job* valida a qualidade do código a cada *commit* através de testes, assim como publica o mesmo para o analisador de testes do GitHub.

1.  **Checkout:** Descarrega o código.
2.  **Setup Docker:** Inicia um *container* Postgres temporário (usando `docker-compose -p plantshop_ci ...`) para os testes de integração.
3.  **Test:** Corre o `dotnet test`, que executa os testes unitários e os testes de integração contra o *container* de teste.
4.  **Publish Results:** Publica um sumário visual dos testes na interface do GitHub.

### 2\. Job: `deploy-to-azure` (Continuous Deployment)

Este *job* (que só corre se o `build-and-test` passar) trata o *deploy* para produção. Aplica as migrações existentes na BD e ainda faz o seed de imagens para o Storage Account.

1.  **Login Azure:** Autentica-se no Azure usando o *Service Principal* (GitHub Secrets).
2.  **Run EF Migrations:** Corre `dotnet ef database update` usando a `AZURE_POSTGRES_CONNECTION`. Isto garante que o esquema da BD de produção está sincronizado com o código.
3.  **Upload Seed Images:** (Se `RUN_IMAGE_SEED: true`). Corre `az storage blob upload-batch` para enviar as imagens dos artigos na pasta `/seed-images` para o Blob Storage.
4.  **Login Docker Hub:** Autentica-se no Docker Hub.
5.  **Build & Push:** Faz build do `Dockerfile` e envia-a para o Docker Hub com as *tags* `latest` e o SHA do *commit*.
6.  **Deploy to App Service:** Corre o `azure/webapps-deploy`, que aponta o App Service para a nova imagem (`...:${{ github.sha }}`) e o reinicia.

-----

### Desenvolvimento Local

A aplicação está configurada para correr localmente num "modo híbrido": utiliza um *container* Docker para a base de dados relacional (PostgreSQL), mas liga-se aos restantes serviços PaaS (CosmosDB, Storage, ServiceBus) no Azure.

1.  **Iniciar Base de Dados Local:** Com o Docker Desktop em execução, correr `docker-compose up -d` na raiz do projeto.
2.  **Aplicar Migrações Locais:** A `ConnectionStrings:DefaultConnection` no `appsettings.json` já aponta para o *container*. Correr o comando:
    
    dotnet ef database update --project .\src\PlantShop.Infrastructure\ --startup-project .\src\PlantShop.WebUI\
    
3.  **Configurar Serviços Azure:** Adicionar as *connection strings* dos serviços Azure (obtidas do Terraform) ao ficheiro `secrets.json` do projeto `PlantShop.WebUI`. Estas irão sobrepor as chaves vazias do `appsettings.json`:
    
    {
      "ConnectionStrings": {
        "StorageAccount": "DefaultEndpointsProtocol=...;AccountKey=...;",
        "ServiceBus": "Endpoint=sb://...;SharedAccessKey=...;",
        "CosmosDb": "AccountEndpoint=https://...;AccountKey=...;"
      }
    }
    
4.  **Executar a Aplicação:** Executar o projeto `PlantShop.WebUI`.

## Credenciais de Administrador

A aplicação é populada (através do `DataSeeder` no arranque) com uma conta de administrador por defeito:

  * **Email:** `admin@plantshop.com`
  * **Password:** `Admin123!`