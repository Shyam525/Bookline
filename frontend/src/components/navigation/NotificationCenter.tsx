import React from 'react';
import { NotificationDropdown } from './NotificationDropdown';

/**
 * @deprecated Use NotificationDropdown directly. NotificationCenter forwards to canonical NotificationDropdown.
 */
export const NotificationCenter: React.FC = () => {
  return <NotificationDropdown />;
};
