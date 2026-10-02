# StockQuoteAlert

Aplicação de console em C# que monitora a cotação de um ativo da B3 e envia um alerta por e-mail quando o preço:

- **sobe acima** do preço de referência para venda: o e-mail aconselha a **venda**;
- **cai abaixo** do preço de referência para compra: o e-mail aconselha a **compra**.

```
> stock-quote-alert.exe PETR4 22.67 22.59
```

## Funcionalidades

- Monitoramento contínuo da cotação usando a API da [brapi](https://brapi.dev).
- Envio de e-mail via SMTP, configurado em um arquivo `appsettings.json`.
- **Sem spam de e-mails:** o alerta é enviado só quando o preço **entra** em uma zona (acima ou abaixo da faixa). Se o preço continuar lá, nenhum e-mail novo é enviado.
- **Horário da B3:** consulta a API apenas com o mercado aberto (seg. a sex., 10h às 18h, horário de Brasília). Fora desse horário, o programa espera até a próxima abertura.
- Validação dos argumentos e do ticker antes de começar.
- Continua rodando mesmo se a API ou o envio do e-mail falharem por um momento.
- Encerramento com **Ctrl+C**.

## Requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Uma conta de e-mail com acesso SMTP. No Gmail é preciso uma [senha de app](https://myaccount.google.com/apppasswords), que exige a verificação em duas etapas ativada.
- *(Opcional)* Um token da [brapi](https://brapi.dev). Sem token, a API só libera os ativos de teste: **PETR4, MGLU3, VALE3 e ITUB4**.

## Configuração

### 1. Clonar o repositório

```powershell
git clone https://github.com/DiegoFonseca256/StockQuoteAlert.git
cd StockQuoteAlert\StockQuoteAlert
```

### 2. Criar o `appsettings.json`

Copie o modelo e preencha com os seus dados:

```powershell
Copy-Item appsettings.example.json appsettings.json
```

```json
{
  "RecipientEmail": "destino_dos_alertas@exemplo.com",
  "IntervalSeconds": 60,
  "Smtp": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "EnableSsl": true,
    "Username": "seu_email@gmail.com",
    "Password": ""
  }
}
```

| Campo | Descrição |
|---|---|
| `RecipientEmail` | E-mail que recebe os alertas |
| `IntervalSeconds` | Tempo entre uma consulta e outra, em segundos |
| `Smtp.Host` / `Smtp.Port` | Servidor SMTP (Gmail: `smtp.gmail.com`, porta `587`) |
| `Smtp.EnableSsl` | Usa conexão segura (TLS) |
| `Smtp.Username` | E-mail que envia os alertas (login no SMTP) |
| `Smtp.Password` | Deixe vazio e defina no `.env` (próximo passo) |

### 3. Criar o `.env` com os segredos

```powershell
Copy-Item .env.example .env
```

```env
BRAPI_API_KEY="seu_token_da_brapi"
Smtp__Password="sua_senha_de_app"
```

Os segredos ficam no `.env`, e não no `appsettings.json`. Qualquer campo da configuração pode ser sobrescrito por uma variável de ambiente, usando `__` para separar os níveis: `Smtp__Password` preenche `Smtp.Password`.

> `appsettings.json` e `.env` estão no `.gitignore`, então suas credenciais não vão para o repositório.

### Como executar



```powershell
dotnet run -- PETR4 22.67 22.59
```


### Argumentos

| Posição | Argumento | Exemplo | Regras |
|---|---|---|---|
| 1 | Ativo (ticker) | `PETR4` | 4 letras + 1 ou 2 números; precisa existir na B3 |
| 2 | Preço de referência para **venda** | `22.67` | maior que zero; aceita `.` ou `,` |
| 3 | Preço de referência para **compra** | `22.59` | maior que zero; menor que o preço de venda |

Se algum argumento for inválido, o programa mostra o erro e encerra com código de saída `1`.

## Exemplo de saída

```
Usando token da variável BRAPI_API_KEY.

Monitorando PETR4: venda acima de 45, compra abaixo de 40.
Pressione Ctrl+C para encerrar.

[10:00:01] PETR4: Preço 44,80 está dentro do intervalo [40, 45]
[10:01:01] PETR4: Preço 45,12 está acima do máximo 45
Enviando e-mail para destino_dos_alertas@exemplo.com com assunto '[VENDA] PETR4 a R$ 45,12'
[10:02:01] PETR4: Preço 45,20 está acima do máximo 45
...
[18:00:01] Mercado fechado. Próxima abertura: 05/10 10:00
```

E-mail enviado:

> **Assunto:** [VENDA] PETR4 a R$ 45,12
>
> A cotação de PETR4 está em R$ 45,12, acima do preço de referência para venda (R$ 45,00).
> Recomendação: VENDER.
>
> Horário: 05/10/2026 10:01:01

## Como funciona

### Regra dos alertas

A cada consulta, o preço é classificado como **abaixo** da faixa, **dentro** dela ou **acima** dela. O programa guarda o último alerta enviado e só envia um novo quando a zona muda:

| Leitura | Zona | Último alerta | E-mail? |
|---|---|---|---|
| 1 | dentro | — | não |
| 2 | acima | — | **sim (venda)** |
| 3 | acima | venda | não (já avisou) |
| 4 | dentro | venda | não (zera o estado) |
| 5 | acima | — | **sim (venda)** |
| 6 | abaixo | venda | **sim (compra)** |

Se o envio falhar, o estado não é atualizado e o programa tenta enviar de novo na próxima consulta.

### Horário de mercado

- Ao iniciar, o programa **sempre faz uma consulta**, mesmo com o mercado fechado, para mostrar a situação atual.
- Depois disso, só consulta de **segunda a sexta, das 10h às 18h** (horário de Brasília, independente do fuso do computador). Essa janela cobre o pregão regular durante o ano todo, já que o horário de fechamento da B3 muda com o horário de verão dos EUA.
- Fora desse horário, o programa mostra a próxima abertura e espera, sem fazer requisições.

## Estrutura do projeto

```
StockQuoteAlert/
├── Program.cs                 # Ponto de entrada: validação e loop de monitoramento
├── AppSettings.cs             # Leitura do appsettings.json + variáveis de ambiente
├── BrapiClient.cs             # Cliente da API brapi
├── EmailService.cs            # Envio de e-mails via SMTP
├── MarketHours.cs             # Horário de funcionamento da B3
├── Validations.cs             # Validação dos argumentos e do ticker
├── appsettings.example.json   # Modelo de configuração
└── .env.example               # Modelo dos segredos
```

## Uso de IA

- **Auxílio no Planejamento:** apoio na organização das etapas do desenvolvimento, comparando os benefícios de cada opção.
- **Explicações:** esclarecimento de conceitos de C# e .NET, para entender o porquê de cada solução.
- **Implementação:** escrita de partes do código sob minha orientação, sempre revisadas e aprovadas por mim.
- **Revisão de código:** sugestões de boas práticas e melhorias de organização, avaliadas antes de serem aplicadas.
- **Testes manuais:** apoio na execução de cenários de teste depois das mudanças, para confirmar o funcionamento do programa.
