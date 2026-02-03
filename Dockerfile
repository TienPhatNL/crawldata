FROM ubuntu:22.04

# Cài dependencies
RUN apt-get update && apt-get install -y \
    curl python3-pip lxc wget unzip \
    && rm -rf /var/lib/apt/lists/*

# Cài Waydroid
RUN curl https://repo.waydro.id/waydroid.gpg | gpg --dearmor -o /usr/share/keyrings/waydroid.gpg
RUN echo "deb [signed-by=/usr/share/keyrings/waydroid.gpg] https://repo.waydro.id/ jammy main" > /etc/apt/sources.list.d/waydroid.list
RUN apt-get update && apt-get install -y waydroid

# Khởi động Waydroid
CMD ["bash", "-c", "waydroid init -f && waydroid container start && waydroid session start && sleep infinity"]