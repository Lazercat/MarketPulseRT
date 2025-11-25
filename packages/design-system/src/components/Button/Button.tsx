import React from "react";
import styled from "@emotion/styled";

const Root = styled.button`
  border-radius: 999px;
  padding: 0.4rem 0.9rem;
  border: none;
  cursor: pointer;
  background: ${({ theme }) => theme.colors.primary};
  color: white;
  font-size: 0.875rem;
  font-weight: 500;
`;

type ButtonProps = React.ButtonHTMLAttributes<HTMLButtonElement>;

export const Button: React.FC<ButtonProps> = (props) => <Root {...props} />;
