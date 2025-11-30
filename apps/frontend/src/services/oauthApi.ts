import type { OAuthProvider, InitiateOAuthRequest, InitiateOAuthResponse } from "../types";
import { config } from "../config";
import { apiRequest } from "../utils/api";

export const oauthApi = {
  async initiateOAuth(
    provider: OAuthProvider,
    baseUrl: string,
    redirectUri: string
  ): Promise<InitiateOAuthResponse> {
    const response = await apiRequest<InitiateOAuthResponse>(
      `${config.api.baseUrl}/api/identity/oauth/${provider.toLowerCase()}/initiate`,
      {
        method: "POST",
        body: JSON.stringify({ baseUrl, redirectUri } as InitiateOAuthRequest),
      }
    );
    return response;
  },
};
