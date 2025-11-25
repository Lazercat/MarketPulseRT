import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import styled from "@emotion/styled";
const PillFrame = styled.span `
  display: inline-flex;
  align-items: center;
  gap: 6px;
  padding: 6px 10px;
  border-radius: 999px;
  font-weight: 600;
  font-size: 12px;
  letter-spacing: 0.01em;
  color: #e7f2ff;
  border: 1px solid rgba(255, 255, 255, 0.12);
  background: ${({ tone }) => {
    switch (tone) {
        case "success":
            return "rgba(82, 255, 203, 0.15)";
        case "warning":
            return "rgba(243, 188, 80, 0.15)";
        case "danger":
            return "rgba(255, 120, 120, 0.15)";
        default:
            return "rgba(255, 255, 255, 0.08)";
    }
}};
  border-color: ${({ tone }) => {
    switch (tone) {
        case "success":
            return "rgba(82, 255, 203, 0.25)";
        case "warning":
            return "rgba(243, 188, 80, 0.25)";
        case "danger":
            return "rgba(255, 120, 120, 0.25)";
        default:
            return "rgba(255, 255, 255, 0.12)";
    }
}};
`;
const Dot = styled.span `
  width: 8px;
  height: 8px;
  border-radius: 50%;
  box-shadow: 0 0 10px currentColor;
  background: ${({ tone }) => {
    switch (tone) {
        case "success":
            return "#52ffcb";
        case "warning":
            return "#f3bc50";
        case "danger":
            return "#ff7878";
        default:
            return "#9db6d8";
    }
}};
`;
export const Pill = ({ tone = "neutral", showDot = true, children, ...rest }) => (_jsxs(PillFrame, { tone: tone, ...rest, children: [showDot && _jsx(Dot, { tone: tone }), children] }));
