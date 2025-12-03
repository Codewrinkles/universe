import { Outlet } from "react-router-dom";
import { ComposerModalProvider, useComposerModal } from "./ComposerModalContext";
import { ComposerModal } from "./ComposerModal";

/**
 * Inner component that renders the modal (needs to be inside Provider to use hook)
 */
function PulseModalRenderer(): JSX.Element {
  const { isOpen, closeModal } = useComposerModal();

  return (
    <ComposerModal
      isOpen={isOpen}
      onClose={closeModal}
    />
  );
}

/**
 * Layout component for all Pulse pages
 * Provides composer modal context and renders modal
 */
export function PulseLayout(): JSX.Element {
  return (
    <ComposerModalProvider>
      <Outlet />
      <PulseModalRenderer />
    </ComposerModalProvider>
  );
}
