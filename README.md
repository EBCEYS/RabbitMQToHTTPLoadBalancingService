# RabbitMQToHTTPLoadBalancingService
## ��������
������ ��� ������������ �������� ����� RabbitMQ � HTTP ��������.

## �������� ����������������� �����
```json
  "AllowedIPs": [ // ������ ai-flask-server-��
    "FirstIp",
    "SecondIp"
  ],
  "RabbitMQConsumers": [ // ������������ ��� Consumers RabbitMQ
    {
      "HostName": "HostName", // ���/����� �����
      "QueueName": "QueueName", // ��� �������
      "UserName": "UserName", // ��� ������������
      "Password": "Password", // ������
      "Timeout": 20 // ������� ��� HTTP ��������
    }
  ]
```