using Rollocracy.Domain.GameRules;

namespace Rollocracy.Infrastructure.Services
{
    internal static class MetricFormulaEngine
    {
        internal sealed class ModifierValue
        {
            public ModifierTargetType TargetType { get; set; }
            public Guid TargetId { get; set; }
            public int AddValue { get; set; }
        }

        internal sealed class MetricComputationRequest
        {
            public required List<MetricDefinition> MetricDefinitions { get; init; }
            public required List<MetricFormulaStep> FormulaSteps { get; init; }
            public required List<MetricComponent> LegacyComponents { get; init; }
            public required Dictionary<Guid, int> BaseAttributeValues { get; init; }
            public required Dictionary<Guid, int> GaugeValues { get; init; }
            public required Dictionary<Guid, int> DerivedStatValues { get; init; }
            public required List<ModifierValue> Modifiers { get; init; }
        }

        public static Dictionary<Guid, int> ComputeAll(MetricComputationRequest request)
        {
            var result = new Dictionary<Guid, int>();
            var visiting = new HashSet<Guid>();

            foreach (var metric in request.MetricDefinitions.OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name))
            {
                ResolveMetricValue(metric.Id, request, result, visiting);
            }

            return result;
        }

        public static int ComputeSingle(MetricComputationRequest request, Guid metricDefinitionId)
        {
            return ResolveMetricValue(metricDefinitionId, request, new Dictionary<Guid, int>(), new HashSet<Guid>());
        }

        private static int ResolveMetricValue(
            Guid metricDefinitionId,
            MetricComputationRequest request,
            Dictionary<Guid, int> cache,
            HashSet<Guid> visiting)
        {
            if (cache.TryGetValue(metricDefinitionId, out var cached))
                return cached;

            if (!visiting.Add(metricDefinitionId))
                throw new InvalidOperationException($"Metric cycle detected for {metricDefinitionId}");

            var definition = request.MetricDefinitions.FirstOrDefault(x => x.Id == metricDefinitionId);
            if (definition == null)
                return 0;

            var steps = request.FormulaSteps
                .Where(x => x.MetricDefinitionId == metricDefinitionId)
                .OrderBy(x => x.Order)
                .ThenBy(x => x.Id)
                .ToList();

            decimal rawValue;

            if (steps.Count > 0)
            {
                rawValue = 0m;

                foreach (var step in steps)
                {
                    var sourceValue = ResolveStepValue(step, request, cache, visiting);
                    rawValue = step.OperationType switch
                    {
                        MetricFormulaOperationType.Add => rawValue + sourceValue,
                        MetricFormulaOperationType.Subtract => rawValue - sourceValue,
                        MetricFormulaOperationType.Multiply => rawValue * sourceValue,
                        MetricFormulaOperationType.Divide => sourceValue == 0m ? throw new InvalidOperationException("Metric formula division by zero") : rawValue / sourceValue,
                        _ => rawValue
                    };
                }
            }
            else
            {
                rawValue = definition.BaseValue;

                foreach (var component in request.LegacyComponents.Where(x => x.MetricDefinitionId == metricDefinitionId))
                {
                    var sourceValue = request.BaseAttributeValues.TryGetValue(component.AttributeDefinitionId, out var value) ? value : 0;
                    rawValue += sourceValue * (component.Weight / 100m);
                }
            }

            rawValue += request.Modifiers
                .Where(x => x.TargetType == ModifierTargetType.Metric && x.TargetId == metricDefinitionId)
                .Sum(x => x.AddValue);

            var rounded = definition.RoundMode switch
            {
                ComputedValueRoundMode.Ceiling => Math.Ceiling(rawValue),
                ComputedValueRoundMode.Floor => Math.Floor(rawValue),
                ComputedValueRoundMode.Nearest => Math.Round(rawValue, MidpointRounding.AwayFromZero),
                ComputedValueRoundMode.None => rawValue,
                _ => rawValue
            };

            var finalValue = (int)rounded;
            finalValue = Math.Clamp(finalValue, definition.MinValue, definition.MaxValue);

            cache[metricDefinitionId] = finalValue;
            visiting.Remove(metricDefinitionId);
            return finalValue;
        }

        private static decimal ResolveStepValue(
            MetricFormulaStep step,
            MetricComputationRequest request,
            Dictionary<Guid, int> cache,
            HashSet<Guid> visiting)
        {
            return step.SourceType switch
            {
                MetricFormulaSourceType.Constant => step.ConstantValue,
                MetricFormulaSourceType.BaseAttribute => step.SourceId.HasValue && request.BaseAttributeValues.TryGetValue(step.SourceId.Value, out var attributeValue) ? attributeValue : 0m,
                MetricFormulaSourceType.Gauge => step.SourceId.HasValue && request.GaugeValues.TryGetValue(step.SourceId.Value, out var gaugeValue) ? gaugeValue : 0m,
                MetricFormulaSourceType.DerivedStat => step.SourceId.HasValue && request.DerivedStatValues.TryGetValue(step.SourceId.Value, out var derivedValue) ? derivedValue : 0m,
                MetricFormulaSourceType.Metric => step.SourceId.HasValue ? ResolveMetricValue(step.SourceId.Value, request, cache, visiting) : 0m,
                _ => 0m
            };
        }
    }
}
