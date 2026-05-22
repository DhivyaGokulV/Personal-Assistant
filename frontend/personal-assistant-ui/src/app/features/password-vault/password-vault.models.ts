export interface EncryptedField { cipherText: string; iv: string; }
export interface VaultStatus { isInitialized: boolean; salt: string | null; verifierCipherText: string | null; verifierIv: string | null; kdfIterations: number | null; }
export interface PasswordGroup { id: string; name: string; description: string | null; entryCount: number; }
export interface PasswordHistory { id: string; changeDate: string; previousPassword: EncryptedField; }
export interface PasswordEntry {
  id: string; groupId: string; groupName: string; name: string; hasUsername: boolean; hasEmail: boolean;
  username: EncryptedField | null; email: EncryptedField | null; password: EncryptedField | null;
  createdDate: string; updatedDate: string | null; history: PasswordHistory[];
}
