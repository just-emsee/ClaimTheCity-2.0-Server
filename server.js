const http = require("http");
const WebSocket = require("ws");

const server = http.createServer((req, res) => {
    if (req.url === "/health") {
        res.writeHead(200);
        res.end("ok");
        return;
    }

    res.writeHead(404);
    res.end();
});

const wss = new WebSocket.Server({ server });

wss.on("connection", (socket) => {
    console.log("Player connected");

    socket.on("message", (data, isBinary) => {
        // Forward the message to other players.
        for (const client of wss.clients) {
            if (
                client !== socket &&
                client.readyState === WebSocket.OPEN
            ) {
                client.send(data, { binary: isBinary });
            }
        }
    });

    socket.on("close", () => {
        console.log("Player disconnected");
    });

    socket.on("error", console.error);
});

const port = process.env.PORT || 55555;

server.listen(port, "0.0.0.0", () => {
    console.log(`Server listening on ${port}`);
});