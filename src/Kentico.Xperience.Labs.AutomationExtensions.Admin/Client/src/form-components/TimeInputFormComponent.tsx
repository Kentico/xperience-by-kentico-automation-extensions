import React, { useMemo, useRef } from "react";
import type { FormComponentProps } from "@kentico/xperience-admin-base";
import { useGlobalization } from "@kentico/xperience-admin-base";
import {
  BorderRadius,
  FormEditMode,
  FormItemWrapper,
  Icon,
  Paper,
  PaperElevation,
  TextWithLabel,
} from "@kentico/xperience-admin-components";
import { Time } from "@internationalized/date";
import { useDateFormatter } from "@react-aria/i18n";
import {
  Button,
  DateInput,
  DateSegment,
  Group,
  I18nProvider,
  ListBox,
  ListBoxItem,
  Popover,
  Select,
  TimeField,
} from "react-aria-components";
import "./TimeInputFormComponent.css";

export const TimeInputFormComponent = (props: FormComponentProps<string>) => {
  const { uiLocale } = useGlobalization();

  return (
    <I18nProvider locale={uiLocale}>
      <TimeInput {...props} />
    </I18nProvider>
  );
};

const TimeInput = (props: FormComponentProps<string>) => {
  const triggerRef = useRef<HTMLDivElement>(null);
  const inputId = `time-input-${props.name}`;
  const disabled = props.editMode === FormEditMode.Disabled;
  const readOnly = props.editMode === FormEditMode.ReadOnly;
  const value = useMemo(() => {
    if (!props.value || !/^([01]\d|2[0-3]):[0-5]\d$/.test(props.value)) {
      return null;
    }

    const [hour, minute] = props.value.split(":").map(Number);
    return new Time(hour, minute);
  }, [props.value]);
  const formatter = useDateFormatter({
    hour: "numeric",
    minute: "numeric",
    timeZone: "UTC",
  });
  const options = useMemo(() => {
    const format = (time: Time) =>
      formatter.format(new Date(Date.UTC(2000, 0, 1, time.hour, time.minute)));
    const items = Array.from({ length: 96 }, (_, index) => {
      const time = new Time(Math.floor(index / 4), (index % 4) * 15);
      return { id: time.toString().slice(0, 5), label: format(time) };
    });

    // Keep saved or typed non-quarter-hour values selectable in the dropdown.
    if (value && value.minute % 15 !== 0) {
      items.push({ id: value.toString().slice(0, 5), label: format(value) });
      items.sort((left, right) => left.id.localeCompare(right.id));
    }

    return items;
  }, [formatter, value]);

  return (
    <FormItemWrapper
      id={inputId}
      label={props.label}
      markAsRequired={props.required}
      editMode={props.editMode}
      labelIcon={props.tooltip ? "xp-i-circle" : undefined}
      labelIconTooltip={props.tooltip}
      invalid={props.invalid}
      validationMessage={props.validationMessage}
      explanationText={props.explanationText}
    >
      {readOnly ? (
        <TextWithLabel value={props.value} />
      ) : (
        <Select
          className="automation-extensions-time-select"
          aria-label={props.label}
          selectedKey={value?.toString().slice(0, 5) ?? null}
          onSelectionChange={(key) => props.onChange?.(key?.toString() ?? "")}
          isDisabled={disabled}
        >
          <TimeField
            ref={triggerRef}
            id={inputId}
            aria-label={props.label}
            name={props.name}
            value={value}
            onChange={(time) => props.onChange?.(time?.toString().slice(0, 5) ?? "")}
            isDisabled={disabled}
            isRequired={props.required}
            isInvalid={props.invalid}
            validationBehavior="aria"
            granularity="minute"
            data-testid={props.name}
          >
            <Group className="automation-extensions-time-input">
              <DateInput className="automation-extensions-time-segments">
                {(segment) => (
                  <DateSegment
                    segment={segment}
                    className="automation-extensions-time-segment"
                  />
                )}
              </DateInput>
              <Button
                className="automation-extensions-time-button"
                aria-label={props.label}
              >
                <Icon name="xp-chevron-down" />
              </Button>
            </Group>
          </TimeField>
          <Popover
            triggerRef={triggerRef}
            UNSTABLE_portalContainer={triggerRef.current ?? undefined}
            className="automation-extensions-time-popover"
            placement="bottom start"
          >
            <Paper
              elevation={PaperElevation.M}
              borderRadius={BorderRadius.RadiusM}
            >
              <ListBox
                className="automation-extensions-time-options"
                items={options}
                aria-label={props.label}
              >
                {(option) => (
                  <ListBoxItem
                    id={option.id}
                    textValue={option.label}
                    className="automation-extensions-time-option"
                  >
                    {option.label}
                  </ListBoxItem>
                )}
              </ListBox>
            </Paper>
          </Popover>
        </Select>
      )}
    </FormItemWrapper>
  );
};
