# AI-Powered Medical Coding Assistant — System Summary

## Purpose

The Medical Coding Assistant accepts a natural language clinical diagnosis or description (e.g., `"chronic bronchitis and emphysema"`) and returns **relevant ICD-10-CM codes**, prioritized by clinical relevance. The assistant combines **full-text database search** with **LLM-powered interpretation and ranking** to produce context-aware, accurate suggestions.

---

## How It Works

### 1. Initial Input (via REST API)

The user submits:

```json
{
  "query": "chronic bronchitis and emphysema",
  "maxSqlResults": 10
}
```

---

### 2. SQL Full-Text Search

- The system performs a full-text search against a curated ICD-10-CM dataset from [CMS.gov](https://cms.gov) (Centers for Medicare & Medicaid Services).
- The search targets the `LongDescription` field using `CONTAINS` or `FREETEXT` (based on query content).
- Results are ranked by SQL Server’s `RANK` function and limited to `maxSqlResults`.

These initial results act as a **baseline set of potentially relevant codes**.

---

### 3. Prompt Construction for GPT

A user prompt is constructed that includes:

- The **original diagnosis** text
- A **list of ICD-10-CM codes** returned by SQL, formatted with code + description
- An **instructional message** explaining the AI's job:
  - Re-rank the codes based on relevance
  - Add any missing or more appropriate codes
  - Provide a short reason and a confidence score (1–5)
  - Specify the source of each result (`"sql"` or `"gpt"`)

This full prompt is sent to an Azure OpenAI model (currently `gpt-4`) for interpretation.

---

### 4. AI Interpretation and Reranking

The GPT model receives both:
- A **system prompt** (persistent behavioral instruction)
- A **user prompt** (diagnosis + codes + action request)

GPT returns a structured JSON array, such as:

```json
[
  {
    "code": "J410",
    "description": "Simple chronic bronchitis",
    "rank": 1,
    "reason": "Directly matches the chronic bronchitis diagnosis",
    "confidence": 5,
    "source": "sql"
  },
  {
    "code": "J439",
    "description": "Emphysema, unspecified",
    "rank": 2,
    "reason": "Explicitly addresses the emphysema mentioned in the diagnosis",
    "confidence": 5,
    "source": "gpt"
  }
]
```

This list includes **re-ranked SQL results** and **GPT-suggested codes**, merged into a single prioritized output.

---

### 5. Code Normalization and Validation

- All codes are **normalized** (e.g., `J449` instead of `J44.9`) to match official CMS.gov formatting
- Each GPT-suggested code is validated against the ICD-10-CM dataset to flag potential hallucinations
- Codes not present in the dataset are marked as `IsValid: false`

---

### 6. Final Output Structure

The API response includes:

```json
{
  "UsedFreeTextFallback": true,
  "TotalSqlOverallMatchCount": 43,
  "AiModel": "gpt-4",
  "AiVersion": "2025-01-01-preview",
  "AiTemperature": 0.3,
  "SearchResults": [
    {
      "code": "J410",
      "description": "Simple chronic bronchitis",
      "rank": 1,
      "reason": "...",
      "confidence": 5,
      "source": "sql",
      "IsValid": true
    },
    {
      "code": "J439",
      "description": "Emphysema, unspecified",
      "rank": 2,
      "reason": "...",
      "confidence": 5,
      "source": "gpt",
      "IsValid": true
    }
  ]
}
```

---

## Additional Enhancements

The assistant also supports:

- Token usage logging and GPT variability auditing
- Dynamic `maxSqlResults` configuration
- Configurable OpenAI temperature
- Structured logging to file or Application Insights (dev/prod aware)
- AI layer can be swapped out for any LLM which provides a REST API

---

## Recommendation Logic Summary

| Step           | Role                                                      |
|----------------|-----------------------------------------------------------|
| SQL            | Casts a **wide net** using lexical matching               |
| GPT            | Acts as a **semantic filter and enhancer**                |
| Code Validation| Ensures **trustworthiness** by verifying results          |
| Reranking      | Produces an **ordered, explainable** output list          |

---

## Technologies Used

### Backend
- **.NET 8 Azure Function App** (Minimal API)
- **C#** for core logic and integration
- **Azure SQL Database** with full-text indexing

### AI Integration
- **Azure OpenAI** (currently using GPT-4 model)
- Custom system and user prompts
- Configurable model settings (temperature, deployment, etc.)

### Security & Infrastructure
- Hosted on **Azure Linux Consumption Plan**
- **Application Insights** for production monitoring
- **Serilog** for structured local log output
- **API Key Authorization** with environment-based configuration

### Testing & Observability
- MSTest-based unit testing
- LLM token usage logging
- JSONL logging support for offline auditing

### Frontend
- **ASP.NET Razor Pages** frontend ([separate repo](https://github.com/jerhow/AIMCA-UI-Razor))
- Hosted separately in **Azure App Service**

---

## Summary

This system combines the speed of SQL full-text search with the semantic intelligence of a GPT-4 model to return highly relevant ICD-10-CM codes for free-text clinical input. The pipeline balances structure, explainability, and accuracy — with logging and configuration suitable for real-world usage and demonstration.

---

## Code
- [Backend](https://github.com/jerhow/AI-medical-coding-assistant)
- [Frontend](https://github.com/jerhow/AIMCA-UI-Razor)

---

## License

This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for details.