# pip install python-dotenv websocket-client
import os
from dotenv import load_dotenv
from websocket import create_connection

# Close connection
def closeConnection(ws):
    ws.close()
    print("WebSocket closed.")

# Create WebSocket connection
def startConnection():
    # Load environment variables from .env
    load_dotenv()

    # Get environment variables
    wsToken = os.getenv("REACT_APP_TOKEN")
    wsProtocol = os.getenv("REACT_APP_PROTOCOL")

    # Check if values are loaded
    if not wsToken or not wsProtocol:
        raise ValueError("Missing REACT_APP_TOKEN or REACT_APP_PROTOCOL in .env file")

    # Use for Raspberry Pi deployment
    # url = f"ws://10.16.4.168:3000?token={wsToken}"

    # Use for local development
    url = f"ws://localhost:3000?token={wsToken}"

    ws = create_connection(url, subprotocols=[wsProtocol])
    print("Connected to server.")
    return ws

# Send printer data to websocket
def transmitData():
    ws = startConnection()
    try:
        with open('data.json', 'r') as file:
            content = file.read()
        print("Sending JSON data...")
        ws.send(content)
    except FileNotFoundError:
        print("Error: data.json not found.")
    finally:
        closeConnection(ws)

if __name__ == "__main__":
    transmitData()
