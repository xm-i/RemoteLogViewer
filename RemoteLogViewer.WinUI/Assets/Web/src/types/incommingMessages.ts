// C#から受信するメッセージの型定義
import { TextLine } from './index';

interface IncomingAbstractWebMessage {
	pageKey: string;
	type: string;
	data: any;
}

export interface IsDisconnectedUpdatedMessage extends IncomingAbstractWebMessage {
	type: 'IsDisconnectedUpdated';
	data: boolean;
}

export interface LineStyleChangedMessage extends IncomingAbstractWebMessage {
	type: 'LineStyleChanged';
	data: string;
}

export interface ReloadRequestedMessage extends IncomingAbstractWebMessage {
	type: 'ReloadRequested';
	data: null;
}

export interface FileOpenedMessage extends IncomingAbstractWebMessage {
	type: 'FileOpened';
	data: {
		pageKey: string;
		tabHeader: string;
	};
}

export interface FileClosedMessage extends IncomingAbstractWebMessage {
	type: 'FileClosed';
	data: string;
}

export interface LoadedMessage extends IncomingAbstractWebMessage {
	type: 'Loaded';
	data: {
		requestId: number;
		content: TextLine[];
	};
}

export interface FileChangedMessage extends IncomingAbstractWebMessage {
	type: 'FileChanged';
	data: string;
}

export interface TotalLinesUpdatedMessage extends IncomingAbstractWebMessage {
	type: 'TotalLinesUpdated';
	data: number;
}

export interface FileLoadProgressUpdatedMessage extends IncomingAbstractWebMessage {
	type: 'FileLoadProgressUpdated';
	data: number;
}

export interface IsFileLoadRunningUpdatedMessage extends IncomingAbstractWebMessage {
	type: 'IsFileLoadRunningUpdated';
	data: boolean;
}

export interface TotalBytesUpdatedMessage extends IncomingAbstractWebMessage {
	type: 'TotalBytesUpdated';
	data: number;
}

export interface OpenedFilePathChangedMessage extends IncomingAbstractWebMessage {
	type: 'OpenedFilePathChanged';
	data: string;
}

export interface SelectedEncodingChangedMessage extends IncomingAbstractWebMessage {
	type: 'SelectedEncodingChanged';
	data: string;
}

export interface IsRangeContentSavingUpdatedMessage extends IncomingAbstractWebMessage {
	type: 'IsRangeContentSavingUpdated';
	data: boolean;
}

export interface SaveRangeProgressUpdatedMessage extends IncomingAbstractWebMessage {
	type: 'SaveRangeProgressUpdated';
	data: number;
}

export interface AvailableEncodingsUpdatedMessage extends IncomingAbstractWebMessage {
	type: 'AvailableEncodingsUpdated';
	data: string[];
}

export interface GrepProgressUpdatedMessage extends IncomingAbstractWebMessage {
	type: 'GrepProgressUpdated';
	data: number;
}

export interface GrepStartLineUpdatedMessage extends IncomingAbstractWebMessage {
	type: 'GrepStartLineUpdated';
	data: number;
}

export interface IsGrepRunningUpdatedMessage extends IncomingAbstractWebMessage {
	type: 'IsGrepRunningUpdated';
	data: boolean;
}

export interface GrepResultAddedMessage extends IncomingAbstractWebMessage {
	type: 'GrepResultAdded';
	data: TextLine[];
}

export interface GrepResultResetMessage extends IncomingAbstractWebMessage {
	type: 'GrepResultReset';
	data: boolean;
}

export type IncomingWebMessage =
	| IsDisconnectedUpdatedMessage
	| LineStyleChangedMessage
	| ReloadRequestedMessage
	| FileOpenedMessage
	| FileClosedMessage
	| LoadedMessage
	| FileChangedMessage
	| TotalLinesUpdatedMessage
	| FileLoadProgressUpdatedMessage
	| IsFileLoadRunningUpdatedMessage
	| TotalBytesUpdatedMessage
	| OpenedFilePathChangedMessage
	| SelectedEncodingChangedMessage
	| IsRangeContentSavingUpdatedMessage
	| SaveRangeProgressUpdatedMessage
	| AvailableEncodingsUpdatedMessage
	| GrepProgressUpdatedMessage
	| GrepStartLineUpdatedMessage
	| IsGrepRunningUpdatedMessage
	| GrepResultAddedMessage
	| GrepResultResetMessage;
