# UrlShortnerAPI

O URL Shortener é um projeto que oferece uma API de encurtamento e gerenciamento de URLs.

## Começando

Essas instruções irão te orientar durante o processo de execução e uso da API.

### Pé requisitos

Para rodar esse container é necessario ter o docker instalado.

* [Windows](https://docs.docker.com/windows/started)
* [OS X](https://docs.docker.com/mac/started/)
* [Linux](https://docs.docker.com/linux/started/)

### Uso

#### Build & Run

```shell
docker compose up -d db
docker compose build
docker compose up csharp_app
```
Como padrão está definida a porta 8080 para chamada da API

As instruções para consumo das APIs estão disposatas no arquivo "Manual_ShortnerUrl.pdf" disposto na raiz do projeto.

## Made with

* Net core Entity Framework
* PostgresSql 12
