# === MCC118 DAQ Current Monitoring with Serial Control and G-code Upload ===

import time
import csv
import threading
import serial
from datetime import datetime
from daqhats import mcc118, hat_list, HatIDs, OptionFlags
import h5py
import numpy as np

# === Configuration ===
DAQ_SAMPLE_RATE_HZ = 50000  # Maximum reliable rate for MCC118 (hardware-paced) is 10 kHz
BUFFER_SIZE_SAMPLES = int(DAQ_SAMPLE_RATE_HZ * 0.1) # Read data in 0.1-second chunks
LOG_FILE = "Data/current_daq_log"
SERIAL_PORT_PRINTER = '/dev/ttyUSB0' # serial port for printer via USB-to-serial 
SERIAL_PORT_ARDUINO = '/dev/serial0' # serial port for arduino via GPIO14 TX to Arduino RX1	
BAUDRATE = 115200
GCODE_FILE = "G-code/Cube_Test_PLA.gcode"
CHANNEL = 0  # Use analog channel 0
SHUNT_RESISTANCE = 0.1  # Ohms
INA_GAIN = 20  # V/V
FLUSH_INTERVAL = 2.0  # seconds - How often to write accumulated data to disk

# === Shared variables ===
write_buffer = []
# Locks for thread safety
daq_buffer_lock = threading.Lock() # Protects access to write_buffer
serial_lock = threading.Lock()     # Protects exclusive access to the serial port
daq_start_event = threading.Event() # Signal when DAQ should start monitoring
daq_stop_event = threading.Event()  # Signal when DAQ should stop monitoring

# === Setup DAQ ===
hat_devices = hat_list(filter_by_id=HatIDs.MCC_118)
if not hat_devices:
    raise RuntimeError("No MCC 118 device found")
hat = mcc118(hat_devices[0].address)
channel_mask = 1 << CHANNEL # Bitmask for channel 0

# === Serial Object (Single instance, globally accessible) ===
ser = None 
pi_uart = None

# === Create HDF5 file and datasets ===
h5file = h5py.File(LOG_FILE, "w")
maxshape = (None,)
chunk_size = BUFFER_SIZE_SAMPLES
h5_time = h5file.create_dataset("Time_s", shape=(0,), maxshape=maxshape, dtype='f4', chunks=(chunk_size,), compression=False, track_times=False)
h5_voltage = h5file.create_dataset("Voltage_V", shape=(0,), maxshape=maxshape, dtype='f4', chunks=(chunk_size,), compression=False, track_times=False)
h5_current = h5file.create_dataset("Current_A", shape=(0,), maxshape=maxshape, dtype='f4', chunks=(chunk_size,), compression=False, track_times=False)

# === DAQ Reader Thread ===
def daq_reader():
    print("DAQ Reader: Waiting for DAQ to start signal from G-code...")
    daq_start_event.wait() # Blocks until daq_start_event is set by upload_gcode

    start_daq_time = time.time() # Reference time for DAQ timestamps

    try:
        # Configure and start hardware-paced scan
        hat.a_in_scan_start(channel_mask, BUFFER_SIZE_SAMPLES, DAQ_SAMPLE_RATE_HZ, OptionFlags.CONTINUOUS)
        print(f"DAQ Reader: Hardware-paced scan started at {DAQ_SAMPLE_RATE_HZ} Hz.")

        while not daq_stop_event.is_set():
            # Read a chunk of samples from the DAQ buffer
            read_result = hat.a_in_scan_read(BUFFER_SIZE_SAMPLES, 0.5) # 0.5s timeout

            if read_result.buffer_overrun > 0:
                print(f"WARNING: DAQ buffer overruns: {read_result.buffer_overruns}")
            
            # Check if any data was read and if scan is still active (important for graceful shutdown)
            if read_result.data and read_result.running:
                current_batch_data = []
                for i, voltage in enumerate(read_result.data):
                    timestamp_s = time.time() - start_daq_time # Simple real-time delta
                    current_a = voltage / (INA_GAIN * SHUNT_RESISTANCE)
                    current_batch_data.append((timestamp_s, voltage, current_a))
                
                with daq_buffer_lock: # Protect shared write buffer
                    write_buffer.extend(current_batch_data)
            else:
                # If no data is available, sleep briefly to prevent busy-waiting without data
                time.sleep(0.001) # Small sleep, or could use a more responsive signaling if DAQ stops mid-scan

    except Exception as e:
        print("DAQ Reader error:", e)
    finally:
        current_scan_status = hat.a_in_scan_status()
        if current_scan_status.running: # Check if scan is still running before stopping
            hat.a_in_scan_stop() # Ensure DAQ scan is stopped
        hat.a_in_scan_cleanup() # Clean up resources
        print("DAQ Reader: Scan stopped and cleaned up.")


# === File Writer Thread ===
def file_writer():
    index = 0
    while not daq_stop_event.is_set():
        time.sleep(FLUSH_INTERVAL)
        with daq_buffer_lock:
            if write_buffer:
                buffer_copy = write_buffer[:]
                del write_buffer[:]

                n_new = len(buffer_copy)
                h5_time.resize((index + n_new,))
                h5_voltage.resize((index + n_new,))
                h5_current.resize((index + n_new,))

                h5_time[index:index + n_new] = [row[0] for row in buffer_copy]
                h5_voltage[index:index + n_new] = [row[1] for row in buffer_copy]
                h5_current[index:index + n_new] = [row[2] for row in buffer_copy]

                index += n_new

    print("File Writer: Exited.")

# === G-code Uploader (now also controlling DAQ) ===
def upload_gcode():
    global ser # Access the shared serial object
    global pi_uart

    try:
        # It's good practice to acquire the lock even for a single-threaded access
        # within this function, though less critical than for multi-threaded access.
        with serial_lock: 
            print(">> Uploading G-code and monitoring for DAQ control commands...")
            with open(GCODE_FILE, "r") as gcode:
                for line in gcode:
                    line_to_send = line.strip()
                    if line_to_send and not line_to_send.startswith(";"): # Ignore empty lines and comments
                        # Check for DAQ control commands BEFORE sending
                        if "M118 START_MONITOR" in line_to_send:
                            daq_start_event.set() # Signal DAQ to start
                            print(">> Detected START_MONITOR in G-code. Signaled DAQ to start.")
                        elif "M118 STOP_MONITOR" in line_to_send:
                            daq_stop_event.set() # Signal DAQ to stop
                            print(">> Detected STOP_MONITOR in G-code. Signaled DAQ to stop.")

                        ser.write((line_to_send + "\n").encode('utf-8')) # write to printer
                        pi_uart.write((line_to_send + "\n").encode('utf-8')) # write to arduino
                        # --- MODIFICACIÓN CLAVE AQUÍ ---
                        received_ok = False
                        # Set a much longer timeout for expecting 'ok' (e.g., 60 seconds)
                        # This overall loop timeout is separate from ser.timeout
                        start_response_wait_time = time.time() 
                        
                        while not received_ok:
                            # Read one line from the printer. This respects ser.timeout.
                            # If ser.timeout is 1 second, it will wait up to 1 second for a line.
                            # If it gets a line, it processes it. If not, it loops and checks overall timeout.
                            line_from_printer = ser.readline().decode('utf-8', errors='ignore').strip()
                            
                            if line_from_printer:
                                # Optional: print printer responses for debugging
                                # print(f"  <-- {line_from_printer}") 
                                
                                if "ok" in line_from_printer:
                                    received_ok = True
                                elif "resend" in line_from_printer:
                                    print(f"  !!! PRINTER REQUESTED RESEND for '{line_to_send}'. Response: {line_from_printer}")
                                    # For a real solution, you'd implement logic to resend the line.
                                    # For now, we raise an error to stop.
                                    raise IOError("Printer requested resend.")
                                elif "error" in line_from_printer.lower(): # Catch general errors
                                     print(f"  !!! PRINTER ERROR: {line_from_printer} after sending: {line_to_send}")
                                     raise IOError(f"Printer error detected: {line_from_printer}")
                                # Add more checks for other important status messages if your printer sends them
                                # e.g., if "busy" in line_from_printer: pass (ignore busy messages)
                            
                            # Check if overall timeout has been reached for this command
                            if time.time() - start_response_wait_time > 60: # 60 seconds general timeout
                                raise TimeoutError(f"Timeout waiting for 'ok' response for G-code line: {line_to_send}")
                            
                            # Small sleep to prevent busy-waiting if nothing is in buffer immediately
                            # (though readline() is blocking for ser.timeout, this is a safeguard for outer loop)
                            time.sleep(0.001)

            print(">> G-code upload complete.")
            # Ensure DAQ stop event is set if it wasn't explicitly in G-code
            if daq_start_event.is_set() and not daq_stop_event.is_set():
                 print(">> G-code finished, but STOP_MONITOR not detected. Stopping DAQ monitoring.")
                 daq_stop_event.set() 

    except TimeoutError as e:
        print(f"Error uploading G-code: {e}")
        daq_stop_event.set() # Signal stop on upload error
    except Exception as e:
        print(f"Error uploading G-code: {e}")
        daq_stop_event.set() # Signal stop on general upload error


# === Main Execution ===
if __name__ == "__main__":
    try:
        # Initialize serial connection once
        ser = serial.Serial(SERIAL_PORT_PRINTER, BAUDRATE, timeout=5)
        time.sleep(2) # Give serial port time to initialize
        pi_uart = serial.Serial(SERIAL_PORT_ARDUINO, BAUDRATE, timeout=5)
        time.sleep(2) # Give serial port time to initialize


        # Start DAQ and file writer threads (they will wait for daq_start_event)
        threading.Thread(target=daq_reader, daemon=True).start()
        threading.Thread(target=file_writer, daemon=True).start()
        
        # Upload G-code (this function now controls DAQ start/stop based on lines sent)
        upload_gcode()

        print("Main: Waiting for DAQ and file writer threads to finish...")
        # Wait until the DAQ stop event is set (signaled by upload_gcode or KeyboardInterrupt)
        daq_stop_event.wait() # Blocks until the event is set

    except KeyboardInterrupt:
        print("\nStopped by user (Ctrl+C).")
    except serial.SerialException as e:
        print(f"Fatal Serial Error: {e}. Check port, baudrate, or if device is connected.")
    except RuntimeError as e:
        print(f"Fatal Hardware Error: {e}. Check DAQ device connection.")
    except Exception as e:
        print(f"An unexpected error occurred in main: {e}")
    finally:
        # Final cleanup and flush
        daq_stop_event.set() # Ensure all threads are signaled to stop (redundant if already set, but safe)
        
        # Give threads a moment to process the stop signal and finish their last tasks
        # File writer needs time to flush remaining data
        time.sleep(FLUSH_INTERVAL + 3) # Wait a bit more than flush interval

        # Ensure serial port is closed
        if ser and ser.is_open:
            ser.close()
            print("Serial port closed.")

            # Final write
        with daq_buffer_lock:
            if write_buffer:
                print("Main: Performing final flush of remaining data...")
                buffer_copy = write_buffer[:]
                del write_buffer[:]
                n_new = len(buffer_copy)
                current_size = h5_time.shape[0]
                h5_time.resize((current_size + n_new,))
                h5_voltage.resize((current_size + n_new,))
                h5_current.resize((current_size + n_new,))
                h5_time[current_size:] = [row[0] for row in buffer_copy]
                h5_voltage[current_size:] = [row[1] for row in buffer_copy]
                h5_current[current_size:] = [row[2] for row in buffer_copy]

        h5file.close()
        print("Data saved to", h5file)
        print("Program finished.")

