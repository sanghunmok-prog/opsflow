import { HttpErrorResponse } from '@angular/common/http';

type StatusMessages = Partial<Record<number, string>>;

export function apiErrorMessage(
  error: unknown,
  fallback: string,
  statusMessages: StatusMessages = {},
): string {
  if (!(error instanceof HttpErrorResponse)) {
    return fallback;
  }

  const statusMessage = statusMessages[error.status];
  if (statusMessage) {
    return statusMessage;
  }

  const serverMessage = extractServerMessage(error.error);
  return serverMessage || fallback;
}

function extractServerMessage(body: unknown): string {
  if (!body || typeof body !== 'object') {
    return '';
  }

  const maybeMessage = (body as { message?: unknown }).message;
  if (typeof maybeMessage === 'string' && maybeMessage.trim()) {
    return maybeMessage.trim();
  }

  const maybeDetail = (body as { detail?: unknown }).detail;
  if (typeof maybeDetail === 'string' && maybeDetail.trim()) {
    return maybeDetail.trim();
  }

  const maybeTitle = (body as { title?: unknown }).title;
  if (typeof maybeTitle === 'string' && maybeTitle.trim()) {
    return maybeTitle.trim();
  }

  const maybeErrors = (body as { errors?: unknown }).errors;
  if (!maybeErrors || typeof maybeErrors !== 'object') {
    return '';
  }

  const firstError = Object.values(maybeErrors as Record<string, unknown>)
    .flatMap((value) => (Array.isArray(value) ? value : [value]))
    .find((value): value is string => typeof value === 'string' && value.trim().length > 0);

  return firstError?.trim() ?? '';
}
