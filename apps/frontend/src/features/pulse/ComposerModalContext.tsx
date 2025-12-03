import { createContext, useContext, useState, ReactNode } from "react";

interface ComposerModalContextType {
  isOpen: boolean;
  openModal: () => void;
  closeModal: () => void;
}

const ComposerModalContext = createContext<ComposerModalContextType | undefined>(undefined);

export interface ComposerModalProviderProps {
  children: ReactNode;
}

/**
 * Provider for managing composer modal state across Pulse pages
 * Allows Post button in navigation to open modal from any Pulse page
 */
export function ComposerModalProvider({ children }: ComposerModalProviderProps): JSX.Element {
  const [isOpen, setIsOpen] = useState(false);

  const openModal = (): void => {
    setIsOpen(true);
  };

  const closeModal = (): void => {
    setIsOpen(false);
  };

  return (
    <ComposerModalContext.Provider value={{ isOpen, openModal, closeModal }}>
      {children}
    </ComposerModalContext.Provider>
  );
}

/**
 * Hook to access composer modal controls
 * Must be used within ComposerModalProvider
 */
export function useComposerModal(): ComposerModalContextType {
  const context = useContext(ComposerModalContext);
  if (!context) {
    throw new Error("useComposerModal must be used within ComposerModalProvider");
  }
  return context;
}
