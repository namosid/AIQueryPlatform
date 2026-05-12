/**
 * useKeyboardShortcuts Hook
 * Manages keyboard shortcuts for modal workspace
 */

import { useEffect } from 'react';
import type { KeyboardShortcut } from '../../types/modal';

type ShortcutMap = {
  [key: string]: () => void;
};

export const useKeyboardShortcuts = (shortcuts: ShortcutMap): void => {
  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      const { key, ctrlKey, metaKey, shiftKey } = event;

      // Check for each shortcut
      Object.entries(shortcuts).forEach(([shortcutKey, handler]) => {
        const parts = shortcutKey.split('+');
        const modifiers = parts.slice(0, -1);
        const mainKey = parts[parts.length - 1];

        const needsCtrl = modifiers.includes('Ctrl') || modifiers.includes('Meta');
        const needsShift = modifiers.includes('Shift');
        const needsAlt = modifiers.includes('Alt');

        const hasCtrl = ctrlKey || metaKey;

        if (
          key === mainKey &&
          (!needsCtrl || hasCtrl) &&
          (!needsShift || shiftKey) &&
          (!needsAlt || event.altKey)
        ) {
          event.preventDefault();
          handler();
        }
      });
    };

    document.addEventListener('keydown', handleKeyDown);

    return () => {
      document.removeEventListener('keydown', handleKeyDown);
    };
  }, [shortcuts]);
};

/**
 * Common keyboard shortcuts
 */
export const SHORTCUTS: KeyboardShortcut[] = [
  {
    key: 'Escape',
    description: 'Close modal',
    handler: () => {},
  },
  {
    key: 'k',
    ctrlKey: true,
    description: 'Focus query input',
    handler: () => {},
  },
  {
    key: 'Enter',
    ctrlKey: true,
    description: 'Submit query',
    handler: () => {},
  },
  {
    key: '/',
    ctrlKey: true,
    description: 'Toggle sidebar',
    handler: () => {},
  },
];
