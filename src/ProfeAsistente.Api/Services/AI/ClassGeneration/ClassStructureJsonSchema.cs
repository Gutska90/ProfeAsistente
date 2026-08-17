namespace ProfeAsistente.Api.Services.AI.ClassGeneration;

/// <summary>JSON Schema describing the expected Gemini response for class structure generation.</summary>
public static class ClassStructureJsonSchema
{
    public const string Schema = """
        {
          "type": "object",
          "required": ["requiresReview", "warnings", "curriculum", "class"],
          "properties": {
            "requiresReview": { "type": "boolean" },
            "warnings": {
              "type": "array",
              "items": { "type": "string" }
            },
            "curriculum": {
              "type": "object",
              "required": ["objectiveId", "objectiveCode", "indicatorIds", "skillIds", "attitudeIds", "transversalObjectiveIds", "curriculumRelease"],
              "properties": {
                "objectiveId": { "type": "string" },
                "objectiveCode": { "type": "string" },
                "indicatorIds": { "type": "array", "items": { "type": "string" } },
                "skillIds": { "type": "array", "items": { "type": "string" } },
                "attitudeIds": { "type": "array", "items": { "type": "string" } },
                "transversalObjectiveIds": { "type": "array", "items": { "type": "string" } },
                "curriculumRelease": { "type": "string" }
              }
            },
            "class": {
              "type": "object",
              "required": ["title", "purpose", "totalDurationMinutes", "start", "development", "closure", "formativeAssessment", "differentiation"],
              "properties": {
                "title": { "type": "string" },
                "purpose": { "type": "string" },
                "totalDurationMinutes": { "type": "integer" },
                "start": { "$ref": "#/$defs/phase" },
                "development": { "$ref": "#/$defs/phase" },
                "closure": { "$ref": "#/$defs/phase" },
                "formativeAssessment": {
                  "type": "object",
                  "properties": {
                    "included": { "type": "boolean" },
                    "strategy": { "type": "string" },
                    "evidence": { "type": "string" },
                    "feedbackMethod": { "type": "string" }
                  }
                },
                "differentiation": {
                  "type": "object",
                  "properties": {
                    "included": { "type": "boolean" },
                    "supportActions": { "type": "array", "items": { "type": "string" } },
                    "extensionActions": { "type": "array", "items": { "type": "string" } },
                    "accessibilityConsiderations": { "type": "array", "items": { "type": "string" } }
                  }
                }
              }
            }
          },
          "$defs": {
            "phase": {
              "type": "object",
              "required": ["durationMinutes", "objective", "teacherActions", "studentActions", "activities", "resources", "evidence"],
              "properties": {
                "durationMinutes": { "type": "integer" },
                "objective": { "type": "string" },
                "teacherActions": { "type": "array", "items": { "type": "string" } },
                "studentActions": { "type": "array", "items": { "type": "string" } },
                "activities": {
                  "type": "array",
                  "items": {
                    "type": "object",
                    "properties": {
                      "name": { "type": "string" },
                      "description": { "type": "string" },
                      "durationMinutes": { "type": "integer" }
                    }
                  }
                },
                "resources": { "type": "array", "items": { "type": "string" } },
                "evidence": { "type": "array", "items": { "type": "string" } }
              }
            }
          }
        }
        """;
}
