# Server Dev 118
PointBlank Server V3.118 Not Complete

<img width="666" height="976" alt="image" src="https://github.com/user-attachments/assets/a9b49ab1-0ef4-42c3-915d-27f04cd3b58d" />


Phase 1 (RAW) — Server sends CONNECT_ACK unencrypted with 5 trailing zeros. It contains the RSA modulus (170 bytes). Both server and client take modulus[16..31] as the SharedKey — used identically for both encrypt and decrypt (symmetric).

Phase 2 (RAW) — Client sends LOGIN_REQ already CMess-encrypted with the SharedKey. Server decrypts it, validates the account, then sends LOGIN_ACK + POINT_CASH_ACK also RAW, because the client is not yet ready to receive encrypted packets at that point. The IsLoginAckSent = true flag is set after these two packets.

Phase 3 (CMess Encrypt) — All subsequent packets are encrypted. CMess format: 4-byte random header + data + padding. SessionMode = SessionId % 3 determines the key derivation method.
All 5 resolved bugs are shown in the diagram — the most critical were the SendCallback bug that called Close() on SocketException (causing 3x red error lines in the log), and the IsLoginAckSent bug that caused LOGIN_ACK to be sent encrypted even though the client had no way to read it yet.
