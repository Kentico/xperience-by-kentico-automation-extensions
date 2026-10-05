import React from "react";
import { FormComponentProps } from "@kentico/xperience-admin-base";
import { FormEditMode, FormItemWrapper } from "@kentico/xperience-admin-components";

// Value is stored as "HH:mm" (24-hour), the native format of <input type="time">.
export const TimeInputFormComponent = (props: FormComponentProps<string>) => {
  const handleOnChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    props.onChange?.(e.target.value);
  };

  const inputId = `time-input-${props.name}`;

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
      <input
        id={inputId}
        type="time"
        name={props.name}
        value={props.value ?? ""}
        onChange={handleOnChange}
        disabled={props.editMode === FormEditMode.Disabled}
        readOnly={props.editMode === FormEditMode.ReadOnly}
        required={props.required}
        data-testid={props.name}
      />
    </FormItemWrapper>
  );
};
