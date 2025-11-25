import { jsx as _jsx } from "react/jsx-runtime";
import styled from "@emotion/styled";
const Frame = styled.div `
  border-radius: 16px;
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid rgba(255, 255, 255, 0.08);
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.25);
`;
export const Card = ({ children, ...rest }) => (_jsx(Frame, { ...rest, children: children }));
