import React from 'react';
import { X, AlertCircle, CheckCircle2, Info, AlertTriangle } from 'lucide-react';
import { clsx } from 'clsx';
import { Button } from '../ui/Button';

export interface ModalProps {
  isOpen: boolean;
  onClose: () => void;
  title: string;
  children: React.ReactNode;
  footer?: React.ReactNode;
}

export const Modal: React.FC<ModalProps> = ({ isOpen, onClose, title, children, footer }) => {
  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-sm animate-fade-in">
      <div className="bg-[#111520] border border-[#212638] rounded-2xl w-full max-w-lg shadow-2xl overflow-hidden flex flex-col max-h-[90vh]">
        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-[#212638]">
          <h3 className="font-heading text-xl font-bold text-white">{title}</h3>
          <button onClick={onClose} className="text-[#7E88A8] hover:text-white transition-colors">
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Content */}
        <div className="p-6 overflow-y-auto flex-1">{children}</div>

        {/* Footer */}
        {footer && <div className="flex items-center justify-end gap-3 px-6 py-4 border-t border-[#212638] bg-[#0A0C13]/50">{footer}</div>}
      </div>
    </div>
  );
};

export interface DrawerProps {
  isOpen: boolean;
  onClose: () => void;
  title: string;
  children: React.ReactNode;
}

export const Drawer: React.FC<DrawerProps> = ({ isOpen, onClose, title, children }) => {
  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex justify-end bg-black/70 backdrop-blur-sm animate-fade-in">
      <div className="bg-[#111520] border-l border-[#212638] w-full max-w-md h-full shadow-2xl flex flex-col">
        <div className="flex items-center justify-between px-6 py-5 border-b border-[#212638]">
          <h3 className="font-heading text-xl font-bold text-white">{title}</h3>
          <button onClick={onClose} className="text-[#7E88A8] hover:text-white transition-colors">
            <X className="w-5 h-5" />
          </button>
        </div>
        <div className="p-6 overflow-y-auto flex-1">{children}</div>
      </div>
    </div>
  );
};

export const Alert: React.FC<{
  variant?: 'info' | 'success' | 'warning' | 'error';
  title?: string;
  children: React.ReactNode;
}> = ({ variant = 'info', title, children }) => {
  const configs = {
    info: { icon: Info, style: 'bg-blue-500/10 border-blue-500/30 text-blue-300' },
    success: { icon: CheckCircle2, style: 'bg-[#34D399]/10 border-[#34D399]/30 text-[#34D399]' },
    warning: { icon: AlertTriangle, style: 'bg-[#FBBF24]/10 border-[#FBBF24]/30 text-[#FBBF24]' },
    error: { icon: AlertCircle, style: 'bg-red-500/10 border-red-500/30 text-red-300' },
  };

  const ConfigIcon = configs[variant].icon;

  return (
    <div className={clsx('flex gap-3 p-4 rounded-xl border text-sm', configs[variant].style)}>
      <ConfigIcon className="w-5 h-5 shrink-0 mt-0.5" />
      <div className="space-y-1">
        {title && <h5 className="font-semibold">{title}</h5>}
        <div>{children}</div>
      </div>
    </div>
  );
};

export const ConfirmDialog: React.FC<{
  isOpen: boolean;
  onClose: () => void;
  onConfirm: () => void;
  title: string;
  message: string;
  confirmText?: string;
  isDanger?: boolean;
}> = ({ isOpen, onClose, onConfirm, title, message, confirmText = 'Confirm', isDanger = false }) => {
  if (!isOpen) return null;

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={title}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button variant={isDanger ? 'danger' : 'primary'} onClick={onConfirm}>
            {confirmText}
          </Button>
        </>
      }
    >
      <p className="text-sm text-[#7E88A8]">{message}</p>
    </Modal>
  );
};
