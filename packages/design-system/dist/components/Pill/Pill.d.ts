import React from "react";
type PillProps = React.HTMLAttributes<HTMLSpanElement> & {
    tone?: "neutral" | "success" | "warning" | "danger";
    showDot?: boolean;
};
export declare const Pill: React.FC<PillProps>;
export {};
