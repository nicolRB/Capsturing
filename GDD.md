# Game Design Document (GDD)

# Capsturing

**Aluno:** Nicolas Robert de Oliveira Borges  
**Email:** nicolas.borges@catolicasc.edu.br  

**Status do Projeto:**
Prototipagem

**Versão de Documento:** v0.2
**Última Atualização:** 07/05/2026

# 1. Visão Geral

## Elevator Pitch

Um jogo 3D de captura de monstro, onde o jogador usa magia para capturar e treinar criaturas sobrenaturais que se tornarão seus aliados em exploração e combate.

## Gênero
- Captura de monstro
- Exploração
- Ação
- Ritmo

## Público Alvo

Jogadores em busca de algo que compense habilidade dentre 16 a 35 anos.

## Plataformas

- PC

# Acesso ao Projeto

| Item | Link |
|-----|-----|
| Build jogável | Itch.io / Download |
| Repositório | GitHub |
| Instruções de execução | Requisitos técnicos |

# 3. Pesquisa e Referências

## Jogos de Referência

- Pokemon
<img src="images/pokemon.jpg" width="300" Height="600">
- Palworld
<img src="images/palworld.jpg" width="300" Height="600">
- Osu
<img src="images/osu.jpg" width="300" Height="600">

## Análise das Referências

Ambos Pokemon e Palworld compartilham do mesmo gênero base de jogo de captura de monstros. Pokemon é famoso por seus designs icônicos e charmosos, e recompensa estratégia em como o jogador faz uso dos traços e capacidades individuais de seus monstros, enquanto Palworld possui mecânicas de gameplay divertidas em como o jogador participa do combate ao lado de seus companheiros. Osu, um jogo de ritmo, é a inspiração principal para as mecânicas de magia de Capsturing, exigindo foco e destreza, para jogar e garantindo bons desafios aos jogadores com sua gameplay punitiva mas que garante uma sensação de triumfo quando se obtem sucesso.

# 4. Hipótese de Design

| Hipótese | Método de Teste |
| Jogadores preferem mecânicas que testem sua habilidade a chance aleatória | Playtest comparando captura baseada em ritmo vs captura puramente probabilísticaPlaytest com mecânica de captura que requer um nivel de habilidade |
| Jogadores conseguem aprender o sistema de captura baseado em ritmo sem tutorial longo | Playtest inicial observando taxa de erro e tempo de aprendizado |
| Usar rúnicos para exploração (montarias ou habilidades) incentiva mais exploração do mapa | Telemetria ou observação do tempo gasto explorando |
| Participação ativa do personagem jogável junto de um rúnico aliado é mais satisfatório comparado a combate com apenas um ou outro | Observação de reação e feedback após capturas ou combates difíceis |

# 5. Gameplay

## Core Loop

Explorar -> Enfrentar ou capturar rúnicos selvagens -> Treinar rúnicos -> Desenvolver novas melhorias -> Continuar Explorando

## Mecânicas Principais

| Mecânica | Descrição |
|-----|-----|
| Movimentação | Andar, correr, pular, montarias |
| Combate | Convocar e comandar rúnico, desviar |
| Interação | Feitiços |

## Regras do Jogo

**Vitória**

Capturar um rúnico de alta raridade.

**Derrota**

HP cai para 0.

**Progressão**

Capturar novos rúnicos, treinar rúnicos capturados, desbloquear novas melhorias

# 6. Escopo do Projeto

## Inclui

- Ao menos 3 regiões para explorar
- Ao menos 12 tipos de rúnicos diferentes
- 3 rúnicos únicos de alta raridade
- Sistema de Captura de rúnicos
- Armazenamento de rúnicos

## Não Inclui

- Sistema de crafting
- Multiplayer
- Geração Procedural
- Mecânicas de sobrevivência
 
# 7. Prototipagem

| Protótipo | Objetivo | Resultado |
| Movimento Básico | Validar Controle | Sob ajustes |
| Minigame de ritmo | Testar timing | Sob ajustes |

## Enredo Base
O personagem jogável, um frágil praticante de magia que visa se tornar um verdadeiro mago, se especializa na arte arriscada e pouco desenvolvida de monsturgia, uma arte de domar e trabalhar com monstros mágicos, chamados rúnicos em seu mundo, que existem fora da lei natural dos animais com habilidades e características sobrenaturais. 

Para que o protagonista se torne um verdadeiro mago, ele precisa demonstrar um nível de aptidão em sua área de estudo através de algum feito considerável relacionado a sua área, e como foi instruído por seu mestre mago, para isto, ele precisará demonstrar que pode domar e treinar algum tipo de rúnico extraordinário, que apenas um professional poderia, como demonstração de estar pronto para estar intitulado de mago.

## Mecânicas do Jogo (RF)
* Movimentação & Exploração: O jogador corre, pula e escala através de um mundo 3D aberto para explorar.
* Combate: O jogador pode ser atacado por, ou atacar, um rúnico, no qual caso sua opção principal sera comandar um rúnico próprio para lutar. O jogador pode tanto desviar para evitar ataques quanto comandar seu rúnico aliado a desviar, comandá-lo a atacar diretamente ou manter-se na defensiva, e pode comandar o rúnico aliado a usar alguma habilidade disponível.
* Captura: O jogador pode capturar um rúnico ao utilizar um feitiço de captura, oque inicia um mini-jogo de ritmo com em que se deve seguir comandos demonstrados de cliques com mouse e tecla sensiveis a timing e, para o mouse, posicionamento, com a precisão no mini-jogo adicionando à probabilidade de o feitiço de captura ter sucesso.

# 8. Interface (UI/UX)
## HUD
- Barra de Vida
- Barra de Vida de Rúnico
- Habilidades de Rúnico
- Seletor de Feitiço
- Seletor de Rúnico
- Minigame de Conjuração

## Menus
- Menu Principal
- Pause
- Game Over
- Configurações
- Inventário
- Menu de Rúnicos
- Menu de Feitiços

## Flow de Menus
<img src="images/Diagrama de Menus.png" width="300" Height="600">

## Controles
<img src="images/keyboard-layout.png" width="300" Height="600">

# 9. Direção Visual

## Direção de Arte
Estilo Fantasia e Cartoon / Anime

<img src="images/chapel3d.png" width="300" Height="600">

<img src="images/water cat.jpg" width="300" Height="600">

---

## Referências Visuais
<img src="images/maga.png" width="300" Height="600">
<img src="images/monstro genshin.jpg" width="300" Height="600">

---

# 10. Áudio

Tipos de áudio utilizados:

- música de fundo
- efeitos sonoros
- sons de animais

---

# 11. Animação

- Animação de Andar
- Animação de Conjuração
- Animação de Desvio
- Animação de Dano
- Animação de Rúnicos

---

# 12. Arquitetura de Software

Scripts separados por responsabilidade

---

## Tecnologias Utilizadas

| Categoria | Ferramenta |
|-----|-----|
| Engine | Unity |
| Linguagem | C# |
| Versionamento | Git + GitHub |
| Assets | Asset Store |

---

# 12. Testes e Playtests

## Playtests

Nenhum playtest por outros foi feito ainda

---
# 13. Cronograma

- Criar os rúnicos e seus comportamentos
- Finalizar sistema de captura
- Criar menu de rúnicos
- Criar sistema de luta entre rúnicos
- Criar sistema de feitiços
- Aplicar audio ao jogo
- Criar mapa
- Criar rúnicos raros

---
# 14. Riscos do Projeto

| Risco | Impacto | Mitigação |
|-----|-----|-----|
| performance baixa | experiência ruim | otimizar ou separar o mapa em áreas separadas |
| choque entre o estilo visual dos rúnicos e o mundo ou personagem jogável | quebra de imersão | adaptar o estilo dos rúnicos para uma mesclagem de fantasia e estilo anime |

---

# 15. Limitações Conhecidas

Por mais que seja tentador, algumas adições não poderão ser implementadas:
- sistema de árvore de habilidades
- dungeons com puzzles
- quests e NPCs

---

# 16. Decisões Importantes

Registro de mudanças relevantes durante o projeto.

| Data | Decisão | Motivo |
|-----|-----|-----|
| março | remover sistema de crafting | escopo muito grande |
| abril | adicionar dash | melhorar mobilidade |

---

# 17. Créditos

Liste assets externos utilizados.

| Recurso | Fonte | Licença |
|-----|-----|-----|
| sprites | AssetStore | EULA |
| efeitos sonoros | pixabay | CC0 |

---

# 18. Reflexão Final
O jogo não foi finalizado ainda
