import { jsx as _jsx } from "react/jsx-runtime";
import styled from "@emotion/styled";
const Root = styled.button `
  border-radius: 999px;
  padding: 0.4rem 0.9rem;
  border: none;
  cursor: pointer;
  background: ${({ theme }) => theme.colors.primary};
  color: white;
  font-size: 0.875rem;
  font-weight: 500;
`;
export const Button = (props) => _jsx(Root, { ...props });
