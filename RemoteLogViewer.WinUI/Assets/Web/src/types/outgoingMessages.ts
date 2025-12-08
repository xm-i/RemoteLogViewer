// C#とのメッセージ通信用の型定義

interface OutgoingAbstractWebMessage {
	pageKey: string;
	requestId: number;
	type: string;
}

export interface ReadyWebMessage extends OutgoingAbstractWebMessage {
	type: 'Ready';
}

export interface RequestWebMessage extends OutgoingAbstractWebMessage {
	type: 'Request';
	start: number;
	end: number;
}

export interface StartGrepWebMessage extends OutgoingAbstractWebMessage {
	type: 'StartGrep';
	startLine: number;
  keyword: string;
  ignoreCase: boolean;
  useRegex: boolean;
}

export interface CancelGrepWebMessage extends OutgoingAbstractWebMessage {
	type: 'CancelGrep';
}

export interface SaveRangeRequestWebMessage extends OutgoingAbstractWebMessage {
	type: 'SaveRangeRequest';
	start: number;
	end: number;
}

export interface ChangeEncodingWebMessage extends OutgoingAbstractWebMessage {
	type: 'ChangeEncoding';
	encoding: string | null;
}

export interface UpdateTotalLineWebMessage extends OutgoingAbstractWebMessage {
	type: 'UpdateTotalLine';
}

export interface FileCloseWebMessage extends OutgoingAbstractWebMessage {
	type: 'FileClose';
}

export type OutgoingWebMessage =
  | ReadyWebMessage
  | RequestWebMessage
  | StartGrepWebMessage
  | CancelGrepWebMessage
  | SaveRangeRequestWebMessage
  | ChangeEncodingWebMessage
  | UpdateTotalLineWebMessage
  | FileCloseWebMessage;
