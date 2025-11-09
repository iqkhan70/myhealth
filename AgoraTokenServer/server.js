import express from "express";
import Redis from "ioredis";
import pkg from "agora-access-token";
const { RtcTokenBuilder, RtcRole } = pkg;

const app = express();
app.use(express.json());

// Redis setup (same password you used in Docker)
const redis = new Redis({
  host: "localhost",
  port: 6379,
  password: "StrongPassword123!"
});

// Agora setup
const AGORA_APP_ID = "efa11b3a7d05409ca979fb25a5b489ae";
const AGORA_APP_CERTIFICATE = "89ab54068fae46aeaf930ffd493e977b";

// GET /realtime/token?channel=myroom&uid=42
app.get("/realtime/token", async (req, res) => {
  try {
    const { channel, uid } = req.query;
    if (!channel || !uid) {
      return res.status(400).json({ error: "Missing channel or uid" });
    }

    const cacheKey = `agora_token:${channel}:${uid}`;

    // ✅ 1️⃣ Try Redis first
    const cached = await redis.get(cacheKey);
    if (cached) {
      console.log(`✅ [Redis] Reusing cached token for ${channel}`);
      return res.json({ token: cached, cached: true });
    }

    // ✅ 2️⃣ Generate new token if not cached
    const expireSeconds = 3600;
    const currentTimestamp = Math.floor(Date.now() / 1000);
    const privilegeExpiredTs = currentTimestamp + expireSeconds;

    const token = RtcTokenBuilder.buildTokenWithUid(
      AGORA_APP_ID,
      AGORA_APP_CERTIFICATE,
      channel,
      parseInt(uid),
      RtcRole.PUBLISHER,
      privilegeExpiredTs
    );

    // ✅ 3️⃣ Cache it
    await redis.set(cacheKey, token, "EX", expireSeconds);
    console.log(`🆕 [Redis] Cached new token for ${channel}`);

    // ✅ 4️⃣ Return to mobile client
    return res.json({ token:"007eJxTYBCfev3xZN7Ihh0a0wy2/ncRPrtJ10dIudy3T21f+L3Xps0KDKlpiYaGScaJ5ikGpiYGlsmJluaWaUlGpommSSYWlompxXcFMhsCGRlErpcwMTJAIIjPwZCcmJMTbxxvyMAAAJUVH8Q=", cached: false });

  } catch (err) {
    console.error("❌ Error generating Agora token:", err);
    return res.status(500).json({ error: err.message });
  }
});

// Health check (optional)
app.get("/health", async (_, res) => {
  const pong = await redis.ping();
  res.json({ redis: pong });
});

const PORT = process.env.PORT || 3000;
app.listen(PORT, () => console.log(`🚀 Token API running on http://localhost:${PORT}`));
