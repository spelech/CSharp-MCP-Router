export interface ExtractedSchemaProperties {
  properties: Record<string, any>;
  required: string[];
  hasSchemaKeywords: boolean;
}

/**
 * Resolves a local JSON schema $ref string (e.g., "#/$defs/MyObject" or "#/definitions/MyObject")
 * within the root schema object.
 */
function resolveLocalRef(ref: string, rootSchema: Record<string, any>): Record<string, any> | null {
  if (!ref.startsWith('#/')) return null;
  const parts = ref.substring(2).split('/');
  let curr: any = rootSchema;
  for (const part of parts) {
    if (curr && typeof curr === 'object' && part in curr) {
      curr = curr[part];
    } else {
      return null;
    }
  }
  return typeof curr === 'object' && curr !== null ? curr : null;
}

/**
 * Traverses and extracts property definitions and required fields from a JSON Schema Draft 2020-12 schema object.
 * Handles properties, allOf, anyOf, oneOf, $ref, $defs / definitions.
 */
export function extractPropertiesFromSchema(
  schema?: Record<string, any> | null
): ExtractedSchemaProperties {
  if (!schema || typeof schema !== 'object' || Object.keys(schema).length === 0) {
    return { properties: {}, required: [], hasSchemaKeywords: false };
  }

  const properties: Record<string, any> = {};
  const requiredSet = new Set<string>();
  let hasComplexSchema = false;

  const rootSchema = schema;

  function traverse(subSchema: Record<string, any> | null, visitedRefs = new Set<string>()) {
    if (!subSchema || typeof subSchema !== 'object') return;

    // Direct properties
    if (subSchema.properties && typeof subSchema.properties === 'object') {
      Object.entries(subSchema.properties).forEach(([key, propSchema]) => {
        if (!properties[key]) {
          properties[key] = propSchema;
        } else if (typeof propSchema === 'object' && propSchema !== null) {
          // Merge descriptions/types if not present
          properties[key] = { ...propSchema, ...properties[key] };
        }
      });
    }

    // Required fields
    if (Array.isArray(subSchema.required)) {
      subSchema.required.forEach((r) => {
        if (typeof r === 'string') requiredSet.add(r);
      });
    }

    // Check non-object type or non-standard complex schema keywords
    if (subSchema.type && subSchema.type !== 'object') {
      hasComplexSchema = true;
    }

    const complexKeywords = [
      'allOf',
      'anyOf',
      'oneOf',
      '$ref',
      'patternProperties',
      'prefixItems',
      'items',
      'dependentRequired',
      'additionalProperties',
    ];
    for (const kw of complexKeywords) {
      if (kw in subSchema) {
        hasComplexSchema = true;
        break;
      }
    }

    // Handle $ref
    if (typeof subSchema.$ref === 'string') {
      const refStr = subSchema.$ref;
      if (!visitedRefs.has(refStr)) {
        visitedRefs.add(refStr);
        const resolved = resolveLocalRef(refStr, rootSchema);
        if (resolved) {
          traverse(resolved, visitedRefs);
        }
      }
    }

    // Handle allOf
    if (Array.isArray(subSchema.allOf)) {
      subSchema.allOf.forEach((item) => {
        if (typeof item === 'object' && item !== null) {
          traverse(item, visitedRefs);
        }
      });
    }

    // Handle anyOf / oneOf
    ['anyOf', 'oneOf'].forEach((subKey) => {
      if (Array.isArray(subSchema[subKey])) {
        subSchema[subKey].forEach((item: any) => {
          if (typeof item === 'object' && item !== null) {
            traverse(item, visitedRefs);
          }
        });
      }
    });

    // Handle patternProperties
    if (subSchema.patternProperties && typeof subSchema.patternProperties === 'object') {
      hasComplexSchema = true;
      Object.entries(subSchema.patternProperties).forEach(([pat, propSchema]) => {
        const fakeName = `[pattern: ${pat}]`;
        if (!properties[fakeName]) {
          properties[fakeName] = propSchema;
        }
      });
    }
  }

  traverse(schema);

  return {
    properties,
    required: Array.from(requiredSet),
    hasSchemaKeywords: hasComplexSchema,
  };
}
