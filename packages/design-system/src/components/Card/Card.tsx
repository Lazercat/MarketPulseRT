import React from "react";
import styled from "@emotion/styled";

const Frame = styled.div`
  border-radius: 16px;
  background: rgba(255, 255, 255, 0.04);
  border: 1px solid rgba(255, 255, 255, 0.08);
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.25);
`;

type CardProps = React.HTMLAttributes<HTMLDivElement>;

export const Card: React.FC<CardProps> = ({ children, ...rest }) => (
  <Frame {...rest}>{children}</Frame>
);
