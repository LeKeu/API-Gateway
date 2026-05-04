import http from 'k6/http';
import { check, sleep } from 'k6';

const TOKEN = open('/scripts/token.txt').trim();

const BASE_URL = 'http://host.docker.internal:5000';

const headers = {
    'Authorization': `Bearer ${TOKEN}`,
    'Content-Type': 'application/json',
}

export const options = {
    scenarios: {
        //cenário 1, aquece o cache (1 usuário, 1 request)
        //fazendo 1 request pra garantir que o cache tá populado antes de medir direto
        warmup: {
            executor: 'shared-iterations',
            vus: 1,
            iterations: 1,
            maxDuration: '10s',
            tags: { scenario: 'warmup' },
        },
        //enário 2, mede cache hit (10 usuários simultâneos, 50 requests)
        cache_hit: {
            executor: 'constant-vus',
            vus: 10,
            duration: '15s',
            startTime: '12s', // começa depois do warmup
            tags: { scenario: 'cache_hit' },
        },
    },
    thresholds: {
        //eu defino os critérios de sucesso:
        //aqui, 95% dos requests do cenário cache_hit devem responder em menos de 100ms
        'http_req_duration{scenario:cache_hit}': ['p(95)<100'],
    },
};

export default function () {
    const res = http.get(`${BASE_URL}/api/produtos`, { headers });

    check(res, {
        'status 200': r => r.status === 200,
        'tem X-Cache header': r =>
            r.headers['X-Cache'] === 'HIT' || r.headers['X-Cache'] === 'MISS',
    });

    sleep(0.1);
}

/*
COMANDO QUE EU USEI NO TEMRINAL PRA RODAR O TESTE
docker run --rm -v C:\Users\Meu user\source\repos\API_Gateway\infra\k6:/scripts grafana/k6 run /scripts/cache-benchmark.js
O token eu guardo no arquivo txt!!
*/