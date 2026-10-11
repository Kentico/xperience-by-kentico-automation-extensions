import React, { useMemo, useState } from "react";
import type { FormComponentProps } from "@kentico/xperience-admin-base";
import { useFormComponentCommandProvider } from "@kentico/xperience-admin-base";
import {
  Button,
  ButtonColor,
  ButtonSize,
  FormEditMode,
  FormItemWrapper,
  Input,
  MenuItem,
  Select,
} from "@kentico/xperience-admin-components";
import "./UrlMatchFormComponent.css";

interface Option {
  readonly value: string;
  readonly text: string;
}

interface OperatorOption extends Option {
  readonly requiresValue: boolean;
  readonly parts: string[];
}

interface UrlMatchFormComponentProps extends FormComponentProps<string> {
  readonly maxRules: number;
  readonly maxValueLength: number;
  readonly maxParameterLength: number;
  readonly parts: Option[];
  readonly operators: OperatorOption[];
  readonly texts: Record<string, string>;
}

interface Rule {
  part: string;
  operator: string;
  value: string;
  parameter: string;
}

interface TestArguments {
  readonly rules: string;
  readonly url: string;
}

interface TestResult {
  readonly isValid: boolean;
  readonly errorMessage?: string;
  readonly isMatch: boolean;
  readonly ruleResults: boolean[];
  readonly timedOut: boolean;
  readonly isRecognized: boolean;
  readonly url?: string;
  readonly host?: string;
  readonly path?: string;
  readonly queryParameters: { name: string; value: string }[];
}

const QUERY_PARAMETER = "QueryParameter";
const MATCHES = "Matches";
const RULES_VERSION = 1;

// Server-side command (UrlMatchComponent.TEST_URL_COMMAND) – the only matcher; nothing is matched in the browser.
const TEST_URL_COMMAND = "TestUrl";

/** Reads stored rules; anything unreadable shows as no rules (the server validation explains the stored value). */
const parseRules = (value: string | undefined): Rule[] => {
  if (!value) {
    return [];
  }

  try {
    const parsed = JSON.parse(value);
    if (!Array.isArray(parsed?.rules)) {
      return [];
    }

    return parsed.rules.map((rule: Partial<Record<keyof Rule, unknown>>) => ({
      part: typeof rule.part === "string" ? rule.part : "",
      operator: typeof rule.operator === "string" ? rule.operator : "",
      value: typeof rule.value === "string" ? rule.value : "",
      parameter: typeof rule.parameter === "string" ? rule.parameter : "",
    }));
  } catch {
    return [];
  }
};

/** Stored format of UrlMatchRuleSet; no rules = empty string (no filter). */
const serializeRules = (rules: Rule[], operators: OperatorOption[]): string => {
  if (rules.length === 0) {
    return "";
  }

  return JSON.stringify({
    version: RULES_VERSION,
    rules: rules.map((rule) => ({
      part: rule.part,
      operator: rule.operator,
      value: requiresValue(operators, rule.operator) ? rule.value : null,
      parameter: rule.part === QUERY_PARAMETER ? rule.parameter : null,
    })),
  });
};

const requiresValue = (operators: OperatorOption[], operator: string) =>
  operators.find((option) => option.value === operator)?.requiresValue ?? true;

const operatorsFor = (operators: OperatorOption[], part: string) =>
  operators.filter((option) => option.parts.includes(part));

const format = (text: string | undefined, value: number) => (text ?? "").replace("{0}", value.toString());

export const UrlMatchFormComponent = (props: UrlMatchFormComponentProps) => {
  const { executeCommand } = useFormComponentCommandProvider();
  const texts = props.texts ?? {};
  const operators = props.operators ?? [];
  const parts = props.parts ?? [];
  const disabled = props.editMode === FormEditMode.Disabled;
  const readOnly = props.editMode === FormEditMode.ReadOnly;
  const rules = useMemo(() => parseRules(props.value), [props.value]);
  const [testUrl, setTestUrl] = useState("");
  const [testResult, setTestResult] = useState<TestResult | null>(null);
  const [testFailed, setTestFailed] = useState(false);
  const [testing, setTesting] = useState(false);

  const update = (next: Rule[]) => {
    setTestResult(null);
    props.onChange?.(serializeRules(next, operators));
  };

  const updateRule = (index: number, change: Partial<Rule>) =>
    update(
      rules.map((rule, current) => {
        if (current !== index) {
          return rule;
        }

        const next = { ...rule, ...change };
        // Keep the operator valid for the part (Exists/DoesNotExist only for query parameters).
        const available = operatorsFor(operators, next.part);
        if (!available.some((option) => option.value === next.operator)) {
          next.operator = available[0]?.value ?? "";
        }

        return next;
      }),
    );

  const addRule = () =>
    update([...rules, { part: "Path", operator: "StartsWith", value: "", parameter: "" }]);

  const removeRule = (index: number) => update(rules.filter((_, current) => current !== index));

  const runTest = async () => {
    setTesting(true);
    setTestFailed(false);
    try {
      const result = await executeCommand<TestResult, TestArguments>(props, TEST_URL_COMMAND, {
        rules: serializeRules(rules, operators),
        url: testUrl,
      });
      setTestResult(result ?? null);
      setTestFailed(!result);
    } catch {
      setTestResult(null);
      setTestFailed(true);
    } finally {
      setTesting(false);
    }
  };

  const partText = (value: string) => parts.find((option) => option.value === value)?.text ?? value;
  const operatorText = (value: string) => operators.find((option) => option.value === value)?.text ?? value;

  return (
    <FormItemWrapper
      id={`url-match-${props.name}`}
      label={props.label}
      markAsRequired={props.required}
      editMode={props.editMode}
      labelIcon={props.tooltip ? "xp-i-circle" : undefined}
      labelIconTooltip={props.tooltip}
      invalid={props.invalid}
      validationMessage={props.validationMessage}
      explanationText={props.explanationText}
      explanationTextAsHtml={props.explanationTextAsHtml}
    >
      <div className="automation-extensions-url-match" data-testid={props.name}>
        {rules.length === 0 && <div className="automation-extensions-url-match-empty">{texts.noRules}</div>}
        {rules.map((rule, index) =>
          readOnly ? (
            <div key={index} className="automation-extensions-url-match-summary">
              {[
                partText(rule.part),
                rule.part === QUERY_PARAMETER ? rule.parameter : undefined,
                operatorText(rule.operator),
                requiresValue(operators, rule.operator) ? rule.value : undefined,
              ]
                .filter((item) => item !== undefined && item !== "")
                .join(" ")}
            </div>
          ) : (
            <div key={index} className="automation-extensions-url-match-rule">
              <div className="automation-extensions-url-match-rule-header">
                <span className="automation-extensions-url-match-rule-title">{format(texts.rule, index + 1)}</span>
                <Button
                  icon="xp-bin"
                  borderless
                  size={ButtonSize.S}
                  color={ButtonColor.Quinary}
                  title={texts.removeRule}
                  disabled={disabled}
                  onClick={() => removeRule(index)}
                />
              </div>
              <div className="automation-extensions-url-match-fields">
                <Select
                  label={texts.part}
                  value={rule.part}
                  disabled={disabled}
                  onChange={(value) => updateRule(index, { part: value ?? "" })}
                >
                  {parts.map((option) => (
                    <MenuItem key={option.value} value={option.value} primaryLabel={option.text} />
                  ))}
                </Select>
                {rule.part === QUERY_PARAMETER && (
                  <Input
                    label={texts.parameter}
                    value={rule.parameter}
                    disabled={disabled}
                    onChange={(event) => updateRule(index, { parameter: event.target.value.slice(0, props.maxParameterLength) })}
                  />
                )}
                <Select
                  label={texts.operator}
                  value={rule.operator}
                  disabled={disabled}
                  onChange={(value) => updateRule(index, { operator: value ?? "" })}
                >
                  {operatorsFor(operators, rule.part).map((option) => (
                    <MenuItem key={option.value} value={option.value} primaryLabel={option.text} />
                  ))}
                </Select>
                {requiresValue(operators, rule.operator) && (
                  <Input
                    label={texts.value}
                    value={rule.value}
                    disabled={disabled}
                    explanationText={rule.operator === MATCHES ? texts.valuePattern : undefined}
                    onChange={(event) => updateRule(index, { value: event.target.value.slice(0, props.maxValueLength) })}
                  />
                )}
              </div>
            </div>
          ),
        )}
        {!readOnly && rules.length < props.maxRules && (
          <div>
            <Button
              label={texts.addRule}
              icon="xp-plus"
              size={ButtonSize.S}
              color={ButtonColor.Secondary}
              disabled={disabled}
              onClick={addRule}
            />
          </div>
        )}
        {!disabled && (
          <div className="automation-extensions-url-match-test">
            <div className="automation-extensions-url-match-test-input">
              <Input
                label={texts.testLabel}
                value={testUrl}
                placeholder={texts.testPlaceholder}
                onChange={(event) => {
                  setTestUrl(event.target.value);
                  setTestResult(null);
                }}
                onKeyPress={(event: React.KeyboardEvent<HTMLInputElement>) => {
                  if (event.key === "Enter") {
                    event.preventDefault();
                    void runTest();
                  }
                }}
              />
              <Button
                label={texts.testButton}
                size={ButtonSize.M}
                color={ButtonColor.Secondary}
                inProgress={testing}
                disabled={testing}
                onClick={() => void runTest()}
              />
            </div>
            {testFailed && <div className="automation-extensions-url-match-result-no">{texts.testFailed}</div>}
            {testResult && <TestResultView result={testResult} rules={rules} texts={texts} />}
          </div>
        )}
      </div>
    </FormItemWrapper>
  );
};

const TestResultView = ({
  result,
  rules,
  texts,
}: {
  readonly result: TestResult;
  readonly rules: Rule[];
  readonly texts: Record<string, string>;
}) => {
  if (!result.isValid) {
    return <div className="automation-extensions-url-match-result-no">{result.errorMessage}</div>;
  }

  const summary = rules.length === 0 ? texts.testNoRules : result.isMatch ? texts.testMatch : texts.testNoMatch;
  const none = texts.testNone;

  return (
    <div className="automation-extensions-url-match-result">
      <div className={result.isMatch ? "automation-extensions-url-match-result-yes" : "automation-extensions-url-match-result-no"}>
        {summary}
      </div>
      {result.ruleResults.length > 0 && (
        <ul className="automation-extensions-url-match-result-rules">
          {result.ruleResults.map((matched, index) => (
            <li key={index}>
              {format(texts.rule, index + 1)}: {matched ? texts.testRuleMatch : texts.testRuleNoMatch}
            </li>
          ))}
        </ul>
      )}
      {result.timedOut && <div className="automation-extensions-url-match-result-no">{texts.testTimedOut}</div>}
      {result.isRecognized ? (
        <dl className="automation-extensions-url-match-parts">
          <dt>{texts.testHost}</dt>
          <dd>{result.host || none}</dd>
          <dt>{texts.testPath}</dt>
          <dd>{result.path || none}</dd>
          <dt>{texts.testQuery}</dt>
          <dd>
            {result.queryParameters.length === 0
              ? none
              : result.queryParameters.map((parameter, index) => (
                  <div key={index}>
                    {parameter.name} = {parameter.value}
                  </div>
                ))}
          </dd>
        </dl>
      ) : (
        <div>{texts.testUnrecognized}</div>
      )}
    </div>
  );
};
